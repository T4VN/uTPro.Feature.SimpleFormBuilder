using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;

namespace uTPro.Feature.SimpleFormBuilder.PropertyEditors;

/// <summary>
/// Server-side schema (Data Editor) backing the "uTPro Form Picker" UI.
///
/// It stores a plain string (the form alias) but adds server-side validation:
/// publishing/saving is blocked — with a single, inline property error (no
/// "cancelled by event" toast) — when the chosen form is no longer available.
/// </summary>
[DataEditor(
    alias: "uTPro.SimpleForm.FormPickerValue",
    ValueType = ValueTypes.String,
    ValueEditorIsReusable = false)]
public class FormPickerDataEditor : DataEditor
{
    public FormPickerDataEditor(IDataValueEditorFactory dataValueEditorFactory)
        : base(dataValueEditorFactory)
    {
    }

    protected override IDataValueEditor CreateValueEditor()
        => DataValueEditorFactory.Create<FormPickerDataValueEditor>(Attribute!);
}
