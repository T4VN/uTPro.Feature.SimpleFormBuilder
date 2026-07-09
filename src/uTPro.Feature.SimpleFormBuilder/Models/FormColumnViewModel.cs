namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// A single column within a group. Width is based on a 12-column grid.
/// </summary>
public class FormColumnViewModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Column width in a 12-column grid (1-12).</summary>
    public int Width { get; set; } = 12;
    /// <summary>Fields in this column, ordered by SortOrder.</summary>
    public List<FormFieldViewModel> Fields { get; set; } = [];
}
