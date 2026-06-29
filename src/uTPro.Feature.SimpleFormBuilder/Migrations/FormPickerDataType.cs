using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace uTPro.Feature.SimpleFormBuilder.Migrations;

/// <summary>
/// Ships a ready-to-use "uTPro Form Picker" Data Type so editors don't have to
/// create one by hand. Created once on startup if it does not already exist.
/// </summary>
public class FormPickerDataTypeComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, EnsureFormPickerDataType>();
}

public class EnsureFormPickerDataType
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Stable key so we can detect the data type across restarts without name lookups.
    private static readonly Guid DataTypeKey = new("f0c1d2e3-4a5b-4c7d-8e9f-0a1b2c3d4e5f");

    private const string DataTypeName = "uTPro Form Picker";
    private const string EditorUiAlias = "uTPro.SimpleForm.FormPicker";
    private const string EditorAlias = "Umbraco.Plain.String"; // backend schema (stores a string)

    private readonly IDataTypeService _dataTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _serializer;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<EnsureFormPickerDataType> _logger;

    public EnsureFormPickerDataType(
        IDataTypeService dataTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer serializer,
        IRuntimeState runtimeState,
        ILogger<EnsureFormPickerDataType> logger)
    {
        _dataTypeService = dataTypeService;
        _propertyEditors = propertyEditors;
        _serializer = serializer;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        try
        {
            // Already present? Nothing to do.
            if (await _dataTypeService.GetAsync(DataTypeKey) is not null)
            {
                return;
            }

            // Resolve the backend property editor (schema) that stores the value.
            if (!_propertyEditors.TryGet(EditorAlias, out IDataEditor? editor) || editor is null)
            {
                _logger.LogWarning("Property editor '{Alias}' not found; skipping Form Picker data type.", EditorAlias);
                return;
            }

            var dataType = new DataType(editor, _serializer)
            {
                Key = DataTypeKey,
                Name = DataTypeName,
                EditorUiAlias = EditorUiAlias,
                DatabaseType = ValueStorageType.Nvarchar,
            };

            var result = await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
            if (result.Success)
            {
                _logger.LogInformation("Created '{Name}' data type.", DataTypeName);
            }
            else
            {
                _logger.LogWarning("Could not create '{Name}' data type: {Status}.", DataTypeName, result.Status);
            }
        }
        catch (Exception ex)
        {
            // Never let optional seeding break site boot.
            _logger.LogWarning(ex, "Failed to ensure the Form Picker data type.");
        }
    }
}
