using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

// Public form submission: file persistence, mass-assignment guard, length/type/pattern
// validation, required-field checks, sensitive-field encryption, and optional storage.
internal partial class uTProSimpleFormService
{
    public (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, IReadOnlyList<FormFileUpload> files, string? ip, string? ua)
    {
        // Files are written to disk only after validation passes; track them so any
        // later failure (or a non-storing form) rolls the writes back — no orphans.
        var savedPaths = new List<string>();
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var form = scope.Database.SingleOrDefault<uTProSimpleFormDto>(
                scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                    .Where<uTProSimpleFormDto>(x => x.Alias == alias));
            if (form == null) return (false, "Form not found");
            if (!form.IsEnabled) return (false, "Form is disabled");

            var allFields = GetAllFormFields(form);

            // Persist uploaded files (validate type/size), mapping each to its file field.
            var fileFields = new Dictionary<string, FormFieldViewModel>(StringComparer.Ordinal);
            foreach (var f in allFields.Where(f => f.Type == "file"))
                fileFields.TryAdd(f.Name, f);

            foreach (var upload in files ?? [])
            {
                if (upload.File == null || upload.File.Length == 0) continue;
                // Ignore files that don't map to a known file field (defensive).
                if (!fileFields.TryGetValue(upload.FieldName, out var field)) continue;

                var (ok, message, value, fullPath) = SaveUploadedFileInternal(form, field, upload.File);
                if (!ok)
                {
                    RollbackFiles(savedPaths);
                    return (false, message);
                }
                savedPaths.Add(fullPath);
                data[upload.FieldName] = value;
            }

            // ── Mass-assignment guard ──
            // Only keep values whose key matches a declared field name; silently drop the rest
            // so a caller can't inject arbitrary columns into the stored entry.
            var declaredNames = new HashSet<string>(
                allFields.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => f.Name),
                StringComparer.Ordinal);
            foreach (var key in data.Keys.Where(k => !declaredNames.Contains(k)).ToList())
                data.Remove(key);

            // Index declared fields for length/type validation.
            var fieldsByName = new Dictionary<string, FormFieldViewModel>(StringComparer.Ordinal);
            foreach (var f in allFields.Where(f => !string.IsNullOrEmpty(f.Name)))
                fieldsByName.TryAdd(f.Name, f);

            // ── Length + type/pattern validation ──
            foreach (var kv in data.ToList())
            {
                if (!fieldsByName.TryGetValue(kv.Key, out var field)) continue;
                // File values are internal storage tokens (already validated at upload) — skip.
                if (field.Type == "file") continue;

                var value = kv.Value;
                if (string.IsNullOrEmpty(value)) continue;

                if (value.Length > MaxFieldLength)
                {
                    RollbackFiles(savedPaths);
                    return (false, $"Field '{field.Label}' exceeds the maximum length of {MaxFieldLength} characters");
                }

                var error = ValidateFieldValue(field, value);
                if (error != null)
                {
                    RollbackFiles(savedPaths);
                    return (false, error);
                }
            }

            // Required-field check. Fields temporarily hidden from the frontend (IsHidden) are
            // not rendered, so they're excluded; every other declared required field must have
            // a non-empty value (including hidden-type inputs, which are still submitted).
            foreach (var f in allFields.Where(f => f.Required && !f.IsHidden))
            {
                if (!data.TryGetValue(f.Name, out var val) || string.IsNullOrWhiteSpace(val))
                {
                    RollbackFiles(savedPaths);
                    return (false, $"Field '{f.Label}' is required");
                }
            }

            // Encrypt sensitive fields
            var sensitiveNames = allFields
                .Where(f => f.IsSensitive || f.Type == "password")
                .Select(f => f.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var storageData = new Dictionary<string, string>(data);
            foreach (var key in storageData.Keys.Where(k => sensitiveNames.Contains(k)).ToList())
            {
                var raw = storageData[key];
                if (!string.IsNullOrEmpty(raw))
                {
                    storageData[key] = EncryptedPrefix + Protector.Protect(raw);
                }
            }

            if (form.StoreEntries)
            {
                var entry = new uTProSimpleFormEntryDto
                {
                    FormId = form.Id,
                    DataJson = JsonSerializer.Serialize(storageData, JsonOpts),
                    IpAddress = ip,
                    UserAgent = ua?.Length > 500 ? ua[..500] : ua,
                    CreatedUtc = DateTime.UtcNow
                };
                scope.Database.Insert(entry);
            }
            else
            {
                // Nothing is persisted for this form, so drop any files we just wrote.
                RollbackFiles(savedPaths);
            }

            return (true, form.SuccessMessage ?? "Thank you for your submission!");
        }
        catch (Exception ex)
        {
            RollbackFiles(savedPaths);
            logger.LogError(ex, "Error submitting form {Alias}", alias);
            return (false, ex.Message);
        }
    }

    // Server-side validation for a submitted value based on its declared field type and any
    // custom validation (regex) pattern. Returns an error message, or null when the value is OK.
    private static string? ValidateFieldValue(FormFieldViewModel field, string value)
    {
        switch (field.Type)
        {
            case "email":
                if (!EmailRegex.IsMatch(value))
                    return $"Field '{field.Label}' must be a valid email address";
                break;
            case "url":
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return $"Field '{field.Label}' must be a valid URL";
                break;
            case "number":
            case "range":
                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    return $"Field '{field.Label}' must be a number";
                break;
        }

        // Honour a custom regex pattern (same one emitted as the HTML "pattern" attribute).
        // HTML patterns match the whole value, so anchor accordingly. A malformed pattern is
        // ignored rather than blocking submissions, and a runaway match is treated as invalid.
        if (!string.IsNullOrWhiteSpace(field.Validation))
        {
            try
            {
                if (!Regex.IsMatch(value, $"^(?:{field.Validation})$",
                        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)))
                    return string.IsNullOrWhiteSpace(field.ValidationMessage)
                        ? $"Field '{field.Label}' is invalid"
                        : field.ValidationMessage;
            }
            catch (ArgumentException) { /* invalid regex configured on the field — skip */ }
            catch (RegexMatchTimeoutException) { return $"Field '{field.Label}' is invalid"; }
        }

        return null;
    }
}
