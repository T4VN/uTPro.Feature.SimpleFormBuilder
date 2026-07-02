using NPoco;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

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
        // Built from the immutable schema snapshots at the bottom of this class so the
        // column types resolve per database provider. The large JSON columns use
        // SpecialDbTypes.NVARCHARMAX (→ nvarchar(max) on SQL Server, text on SQLite and
        // PostgreSQL). The previous AsCustom("NTEXT") emitted a SQL Server-only type that
        // does not exist on PostgreSQL and broke the install there.

        if (TableExists("uTProSimpleFormEntry"))
            Delete.Table("uTProSimpleFormEntry").Do();
        if (TableExists("uTProSimpleForm"))
            Delete.Table("uTProSimpleForm").Do();

        Create.Table<uTProSimpleFormSchema>().Do();
        Create.Table<uTProSimpleFormEntrySchema>().Do();

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

        // Insert via NPoco so table/column identifiers are quoted per provider.
        // A raw INSERT with unquoted PascalCase identifiers fails on PostgreSQL, which
        // folds unquoted identifiers to lower-case.
        Context.Database.Insert(new uTProSimpleFormSchema
        {
            Name = "Contact Us",
            Alias = "contact-us",
            FieldsJson = "[]",                 // legacy, empty
            GroupsJson = groupsJson,
            SuccessMessage = "Thank you for contacting us! We will get back to you soon.",
            RedirectUrl = "",
            EmailTo = "",
            EmailSubject = "New Contact Form Entry",
            StoreEntries = true,
            IsEnabled = true,
            VisibleColumnsJson = null,
            EnableRenderApi = false,
            EnableEntriesApi = false,
            ShowInPicker = true,
            CreatedUtc = now,
            UpdatedUtc = now
        });

        return Task.CompletedTask;
    }

    // ── Immutable schema snapshots ──
    // These describe the table shape for THIS migration only. Per Umbraco guidance they
    // are kept separate from the runtime DTOs (uTProSimpleFormDto) and must not be
    // changed after release; future schema changes belong in new migration steps.

    [TableName("uTProSimpleForm")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    public class uTProSimpleFormSchema
    {
        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        public int Id { get; set; }

        [Column("Name")]
        [Length(255)]
        public string Name { get; set; } = string.Empty;

        [Column("Alias")]
        [Length(255)]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_uTProSimpleForm_Alias")]
        public string Alias { get; set; } = string.Empty;

        [Column("FieldsJson")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string? FieldsJson { get; set; }

        [Column("GroupsJson")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string? GroupsJson { get; set; }

        [Column("SuccessMessage")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(1000)]
        public string? SuccessMessage { get; set; }

        [Column("RedirectUrl")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(500)]
        public string? RedirectUrl { get; set; }

        [Column("EmailTo")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(500)]
        public string? EmailTo { get; set; }

        [Column("EmailSubject")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(500)]
        public string? EmailSubject { get; set; }

        [Column("StoreEntries")]
        [Constraint(Default = "1")]
        public bool StoreEntries { get; set; }

        [Column("IsEnabled")]
        [Constraint(Default = "1")]
        public bool IsEnabled { get; set; }

        [Column("VisibleColumnsJson")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string? VisibleColumnsJson { get; set; }

        [Column("EnableRenderApi")]
        [Constraint(Default = "0")]
        public bool EnableRenderApi { get; set; }

        [Column("EnableEntriesApi")]
        [Constraint(Default = "0")]
        public bool EnableEntriesApi { get; set; }

        [Column("ShowInPicker")]
        [Constraint(Default = "1")]
        public bool ShowInPicker { get; set; }

        [Column("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }

        [Column("UpdatedUtc")]
        public DateTime UpdatedUtc { get; set; }
    }

    [TableName("uTProSimpleFormEntry")]
    [PrimaryKey("id", AutoIncrement = true)]
    [ExplicitColumns]
    public class uTProSimpleFormEntrySchema
    {
        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = true, IdentitySeed = 1)]
        public int Id { get; set; }

        [Column("FormId")]
        public int FormId { get; set; }

        [Column("DataJson")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
        public string? DataJson { get; set; }

        [Column("IpAddress")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(100)]
        public string? IpAddress { get; set; }

        [Column("UserAgent")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(500)]
        public string? UserAgent { get; set; }

        [Column("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }
    }
}
