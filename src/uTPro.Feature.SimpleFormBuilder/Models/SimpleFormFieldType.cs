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
///
/// A custom type may also declare <see cref="Attributes"/>: the builder renders a
/// labelled input for each one (like the built-in Min/Max on Time Picker), and the
/// value is stored in the field's <c>Attributes</c> dictionary — readable in the
/// partial via <c>FieldHelper.Attr(key)</c>. This lets a site add configurable
/// settings for its own field types WITHOUT editing the package.
/// </summary>
public sealed class SimpleFormFieldType
{
    public SimpleFormFieldType(
        string type,
        string label,
        IReadOnlyList<SimpleFormFieldAttribute>? attributes = null)
    {
        Type = type;
        Label = label;
        Attributes = attributes ?? [];
    }

    /// <summary>The field type key. Must match the Razor partial file name.</summary>
    public string Type { get; }

    /// <summary>The human-friendly label shown in the picker.</summary>
    public string Label { get; }

    /// <summary>
    /// Optional custom settings the builder renders as labelled inputs in the field's
    /// settings dialog. Each value is saved into the field's <c>Attributes</c> dictionary.
    /// </summary>
    public IReadOnlyList<SimpleFormFieldAttribute> Attributes { get; }
}

/// <summary>
/// A single configurable setting for a custom <see cref="SimpleFormFieldType"/>.
/// Rendered in the form builder as a labelled input; the entered value is stored in
/// the field's <c>Attributes[Key]</c> and read at render time via <c>FieldHelper.Attr(Key)</c>.
/// </summary>
public sealed class SimpleFormFieldAttribute
{
    public SimpleFormFieldAttribute(
        string key,
        string label,
        string? placeholder = null,
        string inputType = "text")
    {
        Key = key;
        Label = label;
        Placeholder = placeholder;
        InputType = string.IsNullOrWhiteSpace(inputType) ? "text" : inputType;
    }

    /// <summary>Attribute key stored in the field's <c>Attributes</c> dictionary.</summary>
    public string Key { get; }

    /// <summary>Label shown next to the input in the builder.</summary>
    public string Label { get; }

    /// <summary>Optional placeholder text for the input.</summary>
    public string? Placeholder { get; }

    /// <summary>HTML input type (e.g. "text", "number", "password"). Defaults to "text".</summary>
    public string InputType { get; }
}
