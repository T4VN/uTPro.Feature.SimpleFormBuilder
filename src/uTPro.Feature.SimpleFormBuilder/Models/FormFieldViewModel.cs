namespace uTPro.Feature.SimpleFormBuilder.Models;

public class FormFieldViewModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "text";
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? CssClass { get; set; }
    public bool Required { get; set; }
    public string? Validation { get; set; }
    public string? ValidationMessage { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>When true, the field value is encrypted before storage and masked in the backoffice
    /// unless the user has the Sensitive Data permission. Defaults to true for "password" type.</summary>
    public bool IsSensitive { get; set; }
    public List<OptionItem>? Options { get; set; }
    public int SortOrder { get; set; }
    /// <summary>When true, the field is temporarily hidden from the frontend form but kept in the configuration.</summary>
    public bool IsHidden { get; set; }
    /// <summary>Extra attributes JSON for custom field types (e.g. {"siteKey":"xxx"} for turnstile)</summary>
    public Dictionary<string, string>? Attributes { get; set; }
}
