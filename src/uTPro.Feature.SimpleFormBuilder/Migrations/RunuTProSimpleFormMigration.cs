using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace uTPro.Feature.SimpleFormBuilder.Migrations;

/// <summary>
/// Registers the migration handler that runs the uTProSimpleForm migration plan
/// on application start.
/// </summary>
public class RunuTProSimpleFormMigration : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<
            UmbracoApplicationStartedNotification,
            uTProSimpleFormMigrationHandler>();
    }
}
