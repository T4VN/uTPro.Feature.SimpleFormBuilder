using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Redacts sensitive field attribute values before a form model is returned to ANY client
/// (public render/entries APIs and the backoffice list/get endpoints).
///
/// Custom field types can store secrets in <see cref="FormFieldViewModel.Attributes"/> — the
/// canonical example being a Cloudflare Turnstile "secretKey". These must never be serialised
/// back to a browser. We use a deny-list on the attribute *key name* so legitimate public
/// attributes of custom field types keep working, while anything that looks like a credential
/// is stripped.
/// </summary>
public static class FormSanitizer
{
    // Attribute keys that end with "key" but are safe to expose (public, client-side values).
    private static readonly HashSet<string> PublicKeyAttributes =
        new(StringComparer.OrdinalIgnoreCase) { "siteKey", "sitekey" };

    /// <summary>Strips sensitive attributes from every field of the form (in place).</summary>
    public static void SanitizeForOutput(FormViewModel? form)
    {
        if (form == null) return;

        if (form.Fields != null)
            foreach (var field in form.Fields)
                SanitizeField(field);

        if (form.Groups != null)
            foreach (var group in form.Groups)
                if (group.Columns != null)
                    foreach (var column in group.Columns)
                        if (column.Fields != null)
                            foreach (var field in column.Fields)
                                SanitizeField(field);
    }

    /// <summary>Strips sensitive attributes from every form in the collection (in place).</summary>
    public static void SanitizeForOutput(IEnumerable<FormViewModel>? forms)
    {
        if (forms == null) return;
        foreach (var form in forms) SanitizeForOutput(form);
    }

    private static void SanitizeField(FormFieldViewModel? field)
    {
        if (field?.Attributes == null || field.Attributes.Count == 0) return;

        var toRemove = field.Attributes.Keys.Where(IsSensitiveKey).ToList();
        foreach (var key in toRemove)
            field.Attributes.Remove(key);
    }

    // A key is treated as sensitive (case-insensitive) when it contains "secret", "token",
    // "password" or "apikey", OR ends with "key" — except the known-public siteKey/sitekey.
    private static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (PublicKeyAttributes.Contains(key)) return false;

        var k = key.ToLowerInvariant();
        if (k.Contains("secret")) return true;
        if (k.Contains("token")) return true;
        if (k.Contains("password")) return true;
        if (k.Contains("apikey")) return true;
        if (k.EndsWith("key")) return true;
        return false;
    }
}
