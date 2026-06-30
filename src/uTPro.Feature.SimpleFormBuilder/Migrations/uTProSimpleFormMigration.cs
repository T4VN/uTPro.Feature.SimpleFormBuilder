using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;

namespace uTPro.Feature.SimpleFormBuilder.Migrations;

/// <summary>
/// Creates the uTProSimpleForm tables and seeds the default "Contact Us" form.
/// This is a single, clean migration that produces the final schema in one step.
/// </summary>
public class InituTProSimpleForm : AsyncMigrationBase
{
    public InituTProSimpleForm(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        // ── Tables ──

        if (TableExists("uTProSimpleFormEntry"))
            Delete.Table("uTProSimpleFormEntry").Do();
        if (TableExists("uTProSimpleForm"))
            Delete.Table("uTProSimpleForm").Do();

        Create.Table("uTProSimpleForm")
            .WithColumn("Id").AsInt32().NotNullable().Identity().PrimaryKey("PK_uTProSimpleForm")
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("Alias").AsString(255).NotNullable().Unique("IX_uTProSimpleForm_Alias")
            .WithColumn("FieldsJson").AsCustom("NTEXT").Nullable()
            .WithColumn("GroupsJson").AsCustom("NTEXT").Nullable()
            .WithColumn("SuccessMessage").AsString(1000).Nullable()
            .WithColumn("RedirectUrl").AsString(500).Nullable()
            .WithColumn("EmailTo").AsString(500).Nullable()
            .WithColumn("EmailSubject").AsString(500).Nullable()
            .WithColumn("StoreEntries").AsBoolean().WithDefaultValue(true)
            .WithColumn("IsEnabled").AsBoolean().WithDefaultValue(true)
            .WithColumn("VisibleColumnsJson").AsCustom("NTEXT").Nullable()
            .WithColumn("EnableRenderApi").AsBoolean().WithDefaultValue(false)
            .WithColumn("EnableEntriesApi").AsBoolean().WithDefaultValue(false)
            .WithColumn("ShowInPicker").AsBoolean().WithDefaultValue(true)
            .WithColumn("CreatedUtc").AsDateTime().NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime().NotNullable()
            .Do();

        Create.Table("uTProSimpleFormEntry")
            .WithColumn("Id").AsInt32().NotNullable().Identity().PrimaryKey("PK_uTProSimpleFormEntry")
            .WithColumn("FormId").AsInt32().NotNullable()
            .WithColumn("DataJson").AsCustom("NTEXT").Nullable()
            .WithColumn("IpAddress").AsString(100).Nullable()
            .WithColumn("UserAgent").AsString(500).Nullable()
            .WithColumn("CreatedUtc").AsDateTime().NotNullable()
            .Do();

        // ── Seed: Contact Us form (final groups → columns → fields format) ──

        var now = DateTime.UtcNow;

        var groupsJson = @"[
  {
    ""id"":""g1"",""name"":"""",""cssClass"":"""",""sortOrder"":0,
    ""columns"":[
      {""id"":""g1c1"",""width"":6,""fields"":[
        {""id"":""f1"",""type"":""text"",""label"":""Name"",""name"":""name"",""placeholder"":""Name"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter your name""}
      ]},
      {""id"":""g1c2"",""width"":6,""fields"":[
        {""id"":""f2"",""type"":""email"",""label"":""Email"",""name"":""email"",""placeholder"":""Email"",""required"":true,""isSensitive"":true,""sortOrder"":0,""validationMessage"":""Please enter a valid email""}
      ]}
    ]
  },
  {
    ""id"":""g2"",""name"":"""",""cssClass"":"""",""sortOrder"":1,
    ""columns"":[
      {""id"":""g2c1"",""width"":12,""fields"":[
        {""id"":""f3"",""type"":""textarea"",""label"":""Message"",""name"":""message"",""placeholder"":""Message"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter your message""}
      ]}
    ]
  }
]";

        Context.Database.Execute(@"
            INSERT INTO uTProSimpleForm
                (Name, Alias, FieldsJson, GroupsJson,
                 SuccessMessage, RedirectUrl, EmailTo, EmailSubject,
                 StoreEntries, IsEnabled, VisibleColumnsJson,
                 EnableRenderApi, EnableEntriesApi, CreatedUtc, UpdatedUtc)
            VALUES
                (@0, @1, @2, @3,
                 @4, @5, @6, @7,
                 @8, @9, @10,
                 @11, @12, @13, @14)",
            "Contact Us",                                                   // Name
            "contact-us",                                                   // Alias
            "[]",                                                           // FieldsJson (legacy, empty)
            groupsJson,                                                     // GroupsJson
            "Thank you for contacting us! We will get back to you soon.",   // SuccessMessage
            "",                                                             // RedirectUrl
            "",                                                             // EmailTo
            "New Contact Form Entry",                                       // EmailSubject
            true,                                                           // StoreEntries
            true,                                                           // IsEnabled
            null,                                                           // VisibleColumnsJson
            false,                                                          // EnableRenderApi
            false,                                                          // EnableEntriesApi
            now,                                                            // CreatedUtc
            now);                                                           // UpdatedUtc

        return Task.CompletedTask;
    }
}

/// <summary>
/// Kept so the migration plan still recognises installs that already advanced to
/// the "utprosimpleform-showinpicker" state. Idempotent: the column already
/// exists on fresh installs (created by <see cref="InituTProSimpleForm"/>), so it
/// only does work on older databases.
/// </summary>
public class AddShowInPickerColumn : AsyncMigrationBase
{
    public AddShowInPickerColumn(IMigrationContext context) : base(context) { }

    protected override Task MigrateAsync()
    {
        if (!ColumnExists("uTProSimpleForm", "ShowInPicker"))
        {
            Create.Column("ShowInPicker").OnTable("uTProSimpleForm")
                .AsBoolean().NotNullable().WithDefaultValue(true).Do();
        }
        return Task.CompletedTask;
    }
}

// ── Migration runner ──

public class RunuTProSimpleFormMigration : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<
            Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification,
            uTProSimpleFormMigrationHandler>();
    }
}

public class uTProSimpleFormMigrationHandler
    : Umbraco.Cms.Core.Events.INotificationAsyncHandler<
        Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification>
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
        Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification notification,
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
