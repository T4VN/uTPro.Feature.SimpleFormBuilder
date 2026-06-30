# Reference

[← Back to README](../README.md)

## Project Structure

```
uTPro.Feature.SimpleFormBuilder/
├── Controllers/
│   ├── uTProSimpleFormApiController.cs      # Backoffice API (CRUD, entries, field types, permissions)
│   └── uTProSimpleFormSubmitController.cs   # Public API (submit, render, entries)
├── Helpers/
│   ├── FieldPartialResolver.cs              # Finds the right partial for each field type
│   ├── uTProSimpleFormAssets.cs             # Resolves front-end CSS/JS paths
│   └── uTProSimpleFormHtmlHelper.cs         # FieldHelper used in partials
├── Migrations/
│   ├── uTProSimpleFormMigration.cs          # AsyncMigrationBase: creates tables + seeds sample data
│   └── FormPickerDataType.cs                # Ensures the "uTPro Form Picker" data type exists
├── Models/
│   ├── FormModels.cs                        # DTOs, ViewModels, request models
│   └── SimpleFormFieldType.cs               # Field-type descriptor for the custom-type extension point
├── PropertyEditors/
│   └── FormPickerDataEditor.cs              # Server schema for the Form Picker + publish-time value validation
├── Services/
│   ├── uTProSimpleFormService.cs            # Core logic, encryption, entry management
│   └── uTProFormFieldTypeProvider.cs        # IuTProFormFieldTypeProvider + AdduTProSimpleFormFieldType() extension
├── ViewComponents/
│   └── uTProSimpleFormViewComponent.cs      # The @Component.InvokeAsync entry point
├── Views/Partials/uTProSimpleForm/
│   ├── Default.cshtml                        # Main form template
│   └── Fields/                               # One file per field type (+ _Default.cshtml fallback)
└── wwwroot/
    ├── App_Plugins/simple-form/              # Backoffice UI
    │   ├── umbraco-package.json              # Section + sidebar + dashboard + property editor + localization
    │   ├── index.js                          # Dashboard shell: state, lifecycle, guards, render
    │   ├── sidebar.js                        # Left Forms tree (+ / ⋯ menu)
    │   ├── bus.js                            # Event bus shared by sidebar ↔ dashboard
    │   ├── api.js, styles.js, date-range.js  # API helper + styles + pure date helpers
    │   ├── mixins/                           # url-state / form-actions / builder / clipboard / entries
    │   ├── views/                            # list / editor / entries / detail views
    │   ├── property-editor/                  # form-picker.element.js + form-list-config.element.js (content data type UI + settings)
    │   └── lang/en-us.js                     # Localization
    └── uTPro/simple-form/
        ├── css/simple-form.css               # Front-end styles
        └── js/simple-form.js                 # Client-side validation & submission
```

## Static Assets

The package serves its `wwwroot` at the site root (`StaticWebAssetBasePath = /`), so:

- Backoffice assets resolve at `/App_Plugins/simple-form/...` (where Umbraco discovers the package manifest)
- Front-end assets resolve at `/uTPro/simple-form/...`

## Database Tables

Created automatically on first startup.

**`uTProSimpleForm`** — form definitions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Name | nvarchar(255) | Display name |
| Alias | nvarchar(255) | Unique identifier used in code |
| FieldsJson | ntext | Legacy storage (empty for new forms) |
| GroupsJson | ntext | Groups → Columns → Fields as JSON |
| SuccessMessage | nvarchar(1000) | Shown after a successful submission |
| RedirectUrl | nvarchar(500) | Optional redirect after submission |
| EmailTo | nvarchar(500) | Notification recipient |
| EmailSubject | nvarchar(500) | Notification subject |
| StoreEntries | bit | Whether to save submissions |
| IsEnabled | bit | Active or disabled |
| VisibleColumnsJson | ntext | Backoffice entry table config |
| EnableRenderApi | bit | Allow the public render API |
| EnableEntriesApi | bit | Allow the public entries API |
| ShowInPicker | bit | Whether the form appears in the Form Picker data type (default true) |
| CreatedUtc / UpdatedUtc | datetime | Timestamps |

**`uTProSimpleFormEntry`** — submissions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| FormId | int | Links to `uTProSimpleForm` |
| DataJson | ntext | Submitted data (sensitive fields encrypted) |
| IpAddress | nvarchar(100) | Submitter's IP |
| UserAgent | nvarchar(500) | Submitter's browser |
| CreatedUtc | datetime | Submission timestamp |

## Migration Note

Migrations run automatically on startup via Umbraco's migration system (`AsyncMigrationBase`, compatible with Umbraco 16–18). The plan has two steps:

1. `utprosimpleform-init` — creates the `uTProSimpleForm` / `uTProSimpleFormEntry` tables and seeds the sample **Contact Us** form.
2. `utprosimpleform-showinpicker` — ensures the `ShowInPicker` column exists (idempotent; only does work on databases created before that column).

On startup the package also ensures a **uTPro Form Picker** data type exists (created once if missing).

To re-run migrations on a fresh database, delete the row keyed `Umbraco.Core.Upgrader.State+uTPro.uTProSimpleForm` from the `umbracoKeyValue` table (the migration plan is named `uTPro.uTProSimpleForm`).
