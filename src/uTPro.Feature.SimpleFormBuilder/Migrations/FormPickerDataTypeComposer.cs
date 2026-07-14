using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

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
