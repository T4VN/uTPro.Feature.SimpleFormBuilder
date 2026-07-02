using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

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

/// <summary>
/// Executes the uTProSimpleForm migration plan. The plan is keyed by state, so each
/// step runs at most once per database; already-migrated databases are left untouched.
/// </summary>
public class uTProSimpleFormMigrationHandler
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public uTProSimpleFormMigrationHandler(
        ICoreScopeProvider coreScopeProvider,
        IMigrationPlanExecutor migrationPlanExecutor,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState)
    {
        _coreScopeProvider = coreScopeProvider;
        _migrationPlanExecutor = migrationPlanExecutor;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
    }

    public async Task HandleAsync(
        UmbracoApplicationStartedNotification notification,
        CancellationToken cancellationToken)
    {
        if (_runtimeState.Level < RuntimeLevel.Run)
            return;

        var plan = new MigrationPlan("uTPro.uTProSimpleForm");
        plan.From(string.Empty)
            .To<InituTProSimpleForm>("utprosimpleform-init")
            .To<AddShowInPickerColumn>("utprosimpleform-showinpicker");

        var upgrader = new Upgrader(plan);
        await upgrader.ExecuteAsync(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);
    }
}
