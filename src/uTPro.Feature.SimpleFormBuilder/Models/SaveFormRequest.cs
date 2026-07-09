namespace uTPro.Feature.SimpleFormBuilder.Models;

public class SaveFormRequest
{
    public int Id { get; set; }
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
