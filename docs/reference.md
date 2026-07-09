# Reference

[← Back to README](../README.md)

## Project Structure

```
uTPro.Feature.SimpleFormBuilder/
├── Controllers/
│   ├── uTProSimpleFormApiController.cs      # Backoffice API (CRUD, entries, field types, permissions, entry-file download, ZIP export)
│   └── uTProSimpleFormSubmitController.cs   # Public API (submit [JSON or multipart w/ files], render, entries)
├── Helpers/
│   ├── FieldPartialResolver.cs              # Finds the right partial for each field type
│   ├── uTProSimpleFormAssets.cs             # Resolves front-end CSS/JS paths
│   └── uTProSimpleFormHtmlHelper.cs         # FieldHelper used in partials
├── Migrations/
│   ├── InituTProSimpleForm.cs               # AsyncMigrationBase: creates tables + seeds sample data
│   ├── AddShowInPickerColumn.cs             # Idempotent migration adding the ShowInPicker column
│   ├── RunuTProSimpleFormMigration.cs       # Runs the migration plan on startup
│   ├── EnsureFormPickerDataType.cs          # Ensures the "uTPro Form Picker" data type exists
│   └── FormPickerDataTypeComposer.cs        # Wires up the data-type check
├── Models/                                   # One type per file (DTOs, ViewModels, requests)
│   ├── FormViewModel / FormGroupViewModel / FormColumnViewModel / FormFieldViewModel / EntryViewModel …
│   ├── uTProSimpleFormDto.cs / uTProSimpleFormEntryDto.cs   # NPoco table DTOs
│   ├── SimpleFormFieldType.cs               # Field-type descriptor for the custom-type extension point
│   ├── SimpleFormFieldAttribute.cs          # Custom per-field settings declared by custom field types
│   ├── FormSubmissionContext.cs             # State handed to each submission handler
│   ├── FormSubmissionResult.cs              # Continue / Reject result from a handler
│   └── FormSubmissionOptions.cs             # Binds uTPro:Feature:Form (rate-limit settings)
├── PropertyEditors/
│   ├── FormPickerDataEditor.cs              # Server schema for the Form Picker
│   ├── FormPickerDataValueEditor.cs         # Value editor
│   └── FormPickerValueValidator.cs          # Publish-time value validation
├── Services/
│   ├── uTProSimpleFormService.cs            # Core logic, encryption, entry management
│   ├── DIuTProSimpleFormService.cs          # DI registration / composer
│   ├── uTProSimpleFormBuilderExtensions.cs  # AdduTProSimpleFormFieldType() extension
│   ├── IuTProFormFieldTypeProvider.cs + StaticFieldTypeProvider.cs   # Custom field-type registry
│   ├── IFormSubmissionHandler.cs            # Submission-pipeline extension point (v2.3.0+)
│   └── RateLimitSubmissionHandler.cs        # Built-in per-IP + per-form rate limiter
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

## Configuration

All settings are optional — the package ships with safe defaults. Options live under `uTPro:Feature:Form` in `appsettings.json` and are bound to `FormSubmissionOptions`:

| Section | Key | Default | Purpose |
|---|---|---|---|
| `uTPro:Feature:Form:RateLimit` | `Enabled` | `true` | Per-IP + per-form throttling of the public submit endpoint (`v2.3.0+`) |
| `uTPro:Feature:Form:RateLimit` | `PermitLimit` | `5` | Max submissions per window per IP + form |
| `uTPro:Feature:Form:RateLimit` | `WindowSeconds` | `60` | Fixed-window length in seconds |

See [Security & Permissions → Rate limiting](security.md#rate-limiting--anti-spam-v230) for details and reverse-proxy notes.

## Uploaded files storage (`v2.1.0+`)

Files submitted through `file` fields are **not** part of `wwwroot`. They are written to
the app content root under:

```
App_Data/umbraco/Data/uTProSimpleFormUploads/{formAlias}/{yyyyMM}/{guid}{ext}
```

This folder is never served statically. Files are only retrievable via the authenticated
`entry-file` endpoint (or bundled by the ZIP export), and are cleaned up when their entry
or form is deleted. See [Security & Permissions](security.md) for the full model.

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
| DataJson | ntext | Submitted data (sensitive fields encrypted; file fields store a `utpro-file:` reference, not the bytes) |
| IpAddress | nvarchar(100) | Submitter's IP |
| UserAgent | nvarchar(500) | Submitter's browser |
| CreatedUtc | datetime | Submission timestamp |

## Migration Note

Migrations run automatically on startup via Umbraco's migration system (`AsyncMigrationBase`, compatible with Umbraco 16–18). The plan has two steps:

1. `utprosimpleform-init` — creates the `uTProSimpleForm` / `uTProSimpleFormEntry` tables and seeds the sample **Contact Us** form.
2. `utprosimpleform-showinpicker` — ensures the `ShowInPicker` column exists (idempotent; only does work on databases created before that column).

On startup the package also ensures a **uTPro Form Picker** data type exists (created once if missing).

To re-run migrations on a fresh database, delete the row keyed `Umbraco.Core.Upgrader.State+uTPro.uTProSimpleForm` from the `umbracoKeyValue` table (the migration plan is named `uTPro.uTProSimpleForm`).
