namespace uTPro.Feature.SimpleFormBuilder.Models;

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
