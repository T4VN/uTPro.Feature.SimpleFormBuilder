using NPoco;

namespace uTPro.Feature.SimpleFormBuilder.Models;

// ── Database DTOs ──

[TableName("uTProSimpleForm")]
[PrimaryKey("id", AutoIncrement = true)]
public class uTProSimpleFormDto
{
    [Column("id")]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string? FieldsJson { get; set; }
    public string? GroupsJson { get; set; }
    public string? SuccessMessage { get; set; }
    public string? RedirectUrl { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailSubject { get; set; }
    public bool StoreEntries { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public string? VisibleColumnsJson { get; set; }
    public bool EnableRenderApi { get; set; }
    public bool EnableEntriesApi { get; set; }
    /// <summary>When true, the form appears in the content "Form Picker" data type list.</summary>
    public bool ShowInPicker { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
