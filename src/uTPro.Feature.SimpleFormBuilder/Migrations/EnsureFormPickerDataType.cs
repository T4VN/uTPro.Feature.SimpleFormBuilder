using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace uTPro.Feature.SimpleFormBuilder.Migrations;

public class EnsureFormPickerDataType
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    // Stable key so we can detect the data type across restarts without name lookups.
    private static readonly Guid DataTypeKey = new("f0c1d2e3-4a5b-4c7d-8e9f-0a1b2c3d4e5f");

    private const string DataTypeName = "uTPro Form Picker";
    private const string EditorUiAlias = "uTPro.SimpleForm.FormPicker";
    private const string EditorAlias = "uTPro.SimpleForm.FormPickerValue"; // server schema (string + validation)

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
            // Resolve our server-side schema editor.
            if (!_propertyEditors.TryGet(EditorAlias, out IDataEditor? editor) || editor is null)
            {
                _logger.LogWarning("Property editor '{Alias}' not found; skipping Form Picker data type.", EditorAlias);
                return;
            }

            // Upgrade EVERY data type that uses the Form Picker UI but an older
            // backend schema (e.g. our bundled one created before this editor
            // existed, OR data types an editor created by hand). This is what
            // wires up the server-side validation.
            var all = await _dataTypeService.GetAllAsync();
            foreach (var dt in all.Where(d =>
                         string.Equals(d.EditorUiAlias, EditorUiAlias, StringComparison.Ordinal) &&
                         !string.Equals(d.EditorAlias, EditorAlias, StringComparison.Ordinal)))
            {
                dt.Editor = editor;
                await _dataTypeService.UpdateAsync(dt, Constants.Security.SuperUserKey);
                _logger.LogInformation("Upgraded data type '{Name}' to the '{Alias}' editor.", dt.Name, EditorAlias);
            }

            // Ensure the bundled data type exists (create if missing).
            if (await _dataTypeService.GetAsync(DataTypeKey) is not null)
            {
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
