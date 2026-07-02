using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uTPro.Feature.SimpleFormBuilder.Migrations;

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
