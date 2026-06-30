namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// Describes a field type that appears in the backoffice form builder's
/// "Select Field Type" picker.
///
/// Consuming sites (which install the package from NuGet and cannot edit its
/// source) register custom field types via
/// <c>IUmbracoBuilder.AdduTProSimpleFormFieldType(type, label)</c> and provide a
/// matching Razor partial at
/// <c>Views/Partials/uTProSimpleForm/Fields/{type}.cshtml</c>.
/// </summary>
public sealed class SimpleFormFieldType
{
    public SimpleFormFieldType(string type, string label)
    {
        Type = type;
        Label = label;
    }

    /// <summary>The field type key. Must match the Razor partial file name.</summary>
    public string Type { get; }

    /// <summary>The human-friendly label shown in the picker.</summary>
    public string Label { get; }
}
