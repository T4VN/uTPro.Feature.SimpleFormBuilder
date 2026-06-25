using System.Text.Json.Serialization;
using NPoco;

namespace uTPro.Feature.SimpleFormBuilder.Models;

// ── Database DTOs ──

[TableName("utpro_uTProSimpleForm")]
[PrimaryKey("Id", AutoIncrement = true)]
public class uTProSimpleFormDto
{
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
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

[TableName("utpro_uTProSimpleFormEntry")]
[PrimaryKey("Id", AutoIncrement = true)]
public class uTProSimpleFormEntryDto
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string? DataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedUtc { get; set; }
}

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
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

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

public class OptionItem
{
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

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
}

public class DeleteFormRequest
{
    public int Id { get; set; }
}

public class GetFormRequest
{
    public int Id { get; set; }
}

public class SubmitFormRequest
{
    public string Alias { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = [];
}

public class EntryViewModel
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];
    public string? IpAddress { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class EntryListRequest
{
    public int FormId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}
