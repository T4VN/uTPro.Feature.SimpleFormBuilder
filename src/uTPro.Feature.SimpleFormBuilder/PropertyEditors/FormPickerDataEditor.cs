using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Validation;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using uTPro.Feature.SimpleFormBuilder.Services;

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

public class FormPickerDataValueEditor : DataValueEditor
{
    public FormPickerDataValueEditor(
        IShortStringHelper shortStringHelper,
        IJsonSerializer jsonSerializer,
        IIOHelper ioHelper,
        DataEditorAttribute attribute,
        IuTProSimpleFormService formService)
        : base(shortStringHelper, jsonSerializer, ioHelper, attribute)
        => Validators.Add(new FormPickerValueValidator(formService));
}

/// <summary>
/// Validates that the stored form alias still refers to an available form,
/// mirroring the picker UI: the form must exist, have "Show in content picker"
/// enabled, and (when the data type configures an "Allowed forms" whitelist) be
/// part of that whitelist.
/// </summary>
public class FormPickerValueValidator : IValueValidator
{
    private readonly IuTProSimpleFormService _formService;

    public FormPickerValueValidator(IuTProSimpleFormService formService)
        => _formService = formService;

    public IEnumerable<ValidationResult> Validate(
        object? value,
        string? valueType,
        object? dataTypeConfiguration,
        PropertyValidationContext validationContext)
    {
        if (value is not string alias || string.IsNullOrWhiteSpace(alias))
        {
            return []; // empty / (none) is valid
        }

        var form = _formService.GetFormByAlias(alias);
        var available = form is not null && form.ShowInPicker;

        if (available)
        {
            var allowed = GetAllowedForms(dataTypeConfiguration);
            if (allowed is { Count: > 0 } &&
                !allowed.Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase)))
            {
                available = false;
            }
        }

        return available
            ? []
            : [new ValidationResult(
                $"The selected form '{alias}' is no longer available. Choose another form or clear the value.")];
    }

    // Reads the "allowedForms" array from the data type configuration, tolerating
    // the various shapes Umbraco may hand back (non-generic IDictionary covers both
    // Dictionary<string,object> and Dictionary<string,object?>).
    private static IReadOnlyCollection<string>? GetAllowedForms(object? dataTypeConfiguration)
    {
        if (dataTypeConfiguration is not System.Collections.IDictionary config
            || !config.Contains("allowedForms"))
        {
            return null;
        }

        var raw = config["allowedForms"];
        if (raw is null)
        {
            return null;
        }

        try
        {
            if (raw is IEnumerable<string> strings)
            {
                return strings.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }

            var json = JsonSerializer.Serialize(raw);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
        catch
        {
            return null;
        }
    }
}
