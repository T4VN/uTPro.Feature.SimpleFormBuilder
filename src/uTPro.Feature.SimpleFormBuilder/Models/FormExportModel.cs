namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// Portable representation of a form definition (no Id, no entries, no timestamps),
/// used for import/export between sites/environments.
/// </summary>
public class FormExportModel
{
    public string ExportVersion { get; set; } = "1.0";
    public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public List<FormFieldViewModel> Fields { get; set; } = [];
    public List<FormGroupViewModel> Groups { get; set; } = [];
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailSubject { get; set; }
    public bool StoreEntries { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public List<string>? VisibleColumns { get; set; }
    public bool EnableRenderApi { get; set; }
    public bool EnableEntriesApi { get; set; }
    /// <summary>When true, the form appears in the content "Form Picker" data type list.</summary>
    public bool ShowInPicker { get; set; } = true;
}
