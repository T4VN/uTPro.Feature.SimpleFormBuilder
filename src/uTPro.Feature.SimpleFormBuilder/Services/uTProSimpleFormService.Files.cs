using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

// Stored-file lifecycle: validate + save uploads, resolve download paths, and delete files.
internal partial class uTProSimpleFormService
{
    // Validates and writes a single uploaded file for a known file field.
    // Returns the "utpro-file:" value to persist plus the physical path (for rollback).
    private (bool Success, string Message, string Value, string FullPath) SaveUploadedFileInternal(
        uTProSimpleFormDto form, FormFieldViewModel field, IFormFile file)
    {
        // Validate size against the field's "maxSize" (MB) attribute, falling back to a safe
        // default cap when it's unset or non-positive.
        var maxSizeMb = ParseDouble(field.Attributes?.GetValueOrDefault("maxSize"));
        if (maxSizeMb is not > 0) maxSizeMb = DefaultMaxUploadMb;
        if (file.Length > maxSizeMb.Value * 1024 * 1024)
            return (false, $"File for '{field.Label}' exceeds the maximum size of {maxSizeMb.Value:0.##} MB", string.Empty, string.Empty);

        // Validate extension against the field's "accept" attribute (e.g. ".pdf,.jpg").
        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName);
        var accept = field.Attributes?.GetValueOrDefault("accept");
        if (!IsExtensionAllowed(ext, accept))
            return (false, $"File type '{ext}' is not allowed for '{field.Label}'", string.Empty, string.Empty);

        // Persist to a non-public folder: {root}/{alias}/{yyyyMM}/{guid}{ext}
        var safeAlias = SanitizeSegment(form.Alias);
        var subFolder = DateTime.UtcNow.ToString("yyyyMM");
        var targetDir = Path.Combine(FileUploadsRoot, safeAlias, subFolder);
        Directory.CreateDirectory(targetDir);

        var storedName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(targetDir, storedName);
        using (var target = File.Create(fullPath))
        {
            file.CopyTo(target);
        }

        // Relative path is what we protect; the client never sees it.
        var relativePath = string.Join('/', safeAlias, subFolder, storedName);
        var token = FileTokenProtector.Protect(relativePath);
        // Keep the display name free of the delimiter so parsing stays trivial.
        var displayName = originalName.Replace('|', '_');
        var value = $"{FilePrefix}{displayName}|{token}";
        return (true, "Uploaded", value, fullPath);
    }

    // Deletes physical files by absolute path (used to roll back a failed submission).
    private void RollbackFiles(IEnumerable<string> fullPaths)
    {
        foreach (var path in fullPaths)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to roll back uploaded file {Path}", path); }
        }
    }

    public EntryFileResult? GetEntryFile(int entryId, string fieldName, bool canViewSensitive = false)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var entry = scope.Database.SingleOrDefault<uTProSimpleFormEntryDto>(
            scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormEntryDto>()
                .Where<uTProSimpleFormEntryDto>(x => x.Id == entryId));
        if (entry == null || string.IsNullOrEmpty(entry.DataJson)) return null;

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.DataJson, JsonOpts) ?? [];
        if (!data.TryGetValue(fieldName, out var value) || string.IsNullOrEmpty(value)) return null;

        // A file field marked "sensitive" is encrypted first. Mirror the entry-list masking:
        // deny the download to users without the Sensitive Data permission, then peel the layer.
        if (value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            if (!canViewSensitive) return null;
            try { value = Protector.Unprotect(value[EncryptedPrefix.Length..]); }
            catch { return null; }
        }

        if (!TryResolveStoredFilePath(value, out var fullPath, out var displayName)) return null;
        if (!File.Exists(fullPath)) return null;

        if (!ContentTypeProvider.TryGetContentType(displayName, out var contentType))
            contentType = "application/octet-stream";

        var stream = File.OpenRead(fullPath);
        return new EntryFileResult(stream, displayName, contentType);
    }

    // Resolves a stored file-field value into a confined physical path.
    // Handles the optional "sensitive" encryption layer and the protected path token.
    private bool TryResolveStoredFilePath(string? value, out string fullPath, out string displayName)
    {
        fullPath = string.Empty;
        displayName = string.Empty;
        if (string.IsNullOrEmpty(value)) return false;

        var v = value;
        if (v.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            try { v = Protector.Unprotect(v[EncryptedPrefix.Length..]); }
            catch { return false; }
        }

        if (!v.StartsWith(FilePrefix, StringComparison.Ordinal)) return false;

        var body = v[FilePrefix.Length..];
        var sep = body.LastIndexOf('|');
        if (sep < 0) return false;

        displayName = body[..sep];
        var token = body[(sep + 1)..];

        string relativePath;
        try { relativePath = FileTokenProtector.Unprotect(token); }
        catch { return false; }

        // Confine inside the uploads root (defence in depth against traversal,
        // even though the path is protected/signed).
        var root = Path.GetFullPath(FileUploadsRoot);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !full.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            return false;

        fullPath = full;
        return true;
    }

    // Deletes the physical files behind any file-field values in the given set.
    // Safe to call with mixed data — non-file values are ignored.
    private void DeleteStoredFiles(IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            if (!TryResolveStoredFilePath(value, out var fullPath, out _)) continue;
            try { if (File.Exists(fullPath)) File.Delete(fullPath); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete uploaded file {Path}", fullPath); }
        }
    }

    // Reads the raw stored data for entries so their files can be cleaned up on delete.
    private static IEnumerable<string?> ExtractFileValues(string? dataJson)
    {
        if (string.IsNullOrEmpty(dataJson)) return [];
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson, JsonOpts);
        return data == null ? [] : data.Values.Select(v => (string?)v);
    }

    private static bool IsExtensionAllowed(string extension, string? accept)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(accept))
        {
            // No explicit allow-list: fall back to a safe deny-list so a field left unconfigured
            // can't be used to upload executable/script content. Reject extension-less files too.
            if (string.IsNullOrEmpty(ext)) return false;
            return !DangerousExtensions.Contains(ext);
        }

        if (string.IsNullOrEmpty(ext)) return false;
        return accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.TrimStart('.').ToLowerInvariant())
            .Where(a => a.Length > 0 && !a.Contains('/')) // ignore MIME patterns like "image/*"
            .Any(a => a == ext);
    }

    private static double? ParseDouble(string? s)
        => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    // Keep alias usable as a single, safe folder name.
    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((value ?? string.Empty)
            .Select(c => invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray());
        cleaned = cleaned.Trim('.', ' ');
        return string.IsNullOrEmpty(cleaned) ? "form" : cleaned;
    }
}
