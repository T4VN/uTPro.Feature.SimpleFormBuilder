using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Strings;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.PropertyEditors;

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
