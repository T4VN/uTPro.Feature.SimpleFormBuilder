namespace uTPro.Feature.SimpleFormBuilder.Models;

// ── API ViewModels ──

public class FormViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public List<FormFieldViewModel> Fields { get; set; } = [];
    /// <summary>Groups organise fields into visual sections, each with its own grid layout (1-12 columns).</summary>
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
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    /// <summary>Number of stored entries for this form (populated by GetAllForms for the list view).</summary>
    public int EntryCount { get; set; }
}
