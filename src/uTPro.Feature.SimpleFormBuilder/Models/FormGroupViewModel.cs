namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// A visual group/fieldset that contains columns, each with its own width and fields.
/// </summary>
public class FormGroupViewModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Optional group title (rendered as fieldset legend or heading).</summary>
    public string? Name { get; set; }
    /// <summary>Optional CSS class applied to the group wrapper.</summary>
    public string? CssClass { get; set; }
    /// <summary>Columns in this group. Each column has a width (1-12) and its own fields.</summary>
    public List<FormColumnViewModel> Columns { get; set; } = [];
    public int SortOrder { get; set; }
}
