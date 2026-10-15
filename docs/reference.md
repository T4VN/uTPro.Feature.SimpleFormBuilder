> **Rendered documentation:** [docs.utpro.dev](https://docs.utpro.dev/uTPro.Feature.SimpleFormBuilder/) is the canonical, always-current home of this page.

# Reference

[â† Back to README](../README.md)

## Project Structure

```
uTPro.Feature.SimpleFormBuilder/
â”œâ”€â”€ Controllers/
â”‚   â”œâ”€â”€ uTProSimpleFormApiController.cs      # Backoffice API (CRUD, entries, field types, permissions, entry-file download, ZIP export)
â”‚   â””â”€â”€ uTProSimpleFormSubmitController.cs   # Public API (submit [JSON or multipart w/ files], render, entries)
â”œâ”€â”€ Helpers/
â”‚   â”œâ”€â”€ FieldPartialResolver.cs              # Finds the right partial for each field type
â”‚   â”œâ”€â”€ uTProSimpleFormAssets.cs             # Resolves front-end CSS/JS paths
â”‚   â””â”€â”€ uTProSimpleFormHtmlHelper.cs         # FieldHelper used in partials
â”œâ”€â”€ Migrations/
â”‚   â”œâ”€â”€ InituTProSimpleForm.cs               # AsyncMigrationBase: creates tables + seeds sample data
â”‚   â”œâ”€â”€ AddShowInPickerColumn.cs             # Idempotent migration adding the ShowInPicker column
â”‚   â”œâ”€â”€ RunuTProSimpleFormMigration.cs       # Runs the migration plan on startup
â”‚   â”œâ”€â”€ EnsureFormPickerDataType.cs          # Ensures the "uTPro Form Picker" data type exists
â”‚   â””â”€â”€ FormPickerDataTypeComposer.cs        # Wires up the data-type check
â”œâ”€â”€ Models/                                   # One type per file (DTOs, ViewModels, requests)
â”‚   â”œâ”€â”€ FormViewModel / FormGroupViewModel / FormColumnViewModel / FormFieldViewModel / EntryViewModel â€¦
â”‚   â”œâ”€â”€ uTProSimpleFormDto.cs / uTProSimpleFormEntryDto.cs   # NPoco table DTOs
â”‚   â”œâ”€â”€ SimpleFormFieldType.cs               # Field-type descriptor for the custom-type extension point
â”‚   â”œâ”€â”€ SimpleFormFieldAttribute.cs          # Custom per-field settings declared by custom field types
â”‚   â”œâ”€â”€ FormSubmissionContext.cs             # State handed to each submission handler
â”‚   â”œâ”€â”€ FormSubmissionResult.cs              # Continue / Reject result from a handler
â”‚   â””â”€â”€ FormSubmissionOptions.cs             # Binds uTPro:Feature:Form (rate-limit settings)
â”œâ”€â”€ PropertyEditors/
â”‚   â”œâ”€â”€ FormPickerDataEditor.cs              # Server schema for the Form Picker
â”‚   â”œâ”€â”€ FormPickerDataValueEditor.cs         # Value editor
â”‚   â””â”€â”€ FormPickerValueValidator.cs          # Publish-time value validation
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ uTProSimpleFormService.cs            # Core logic, encryption, entry management
â”‚   â”œâ”€â”€ DIuTProSimpleFormService.cs          # DI registration / composer
â”‚   â”œâ”€â”€ uTProSimpleFormBuilderExtensions.cs  # AdduTProSimpleFormFieldType() extension
â”‚   â”œâ”€â”€ IuTProFormFieldTypeProvider.cs + StaticFieldTypeProvider.cs   # Custom field-type registry
â”‚   â”œâ”€â”€ IFormSubmissionHandler.cs            # Submission-pipeline extension point (v2.3.0+)
â”‚   â””â”€â”€ RateLimitSubmissionHandler.cs        # Built-in per-IP + per-form rate limiter
â”œâ”€â”€ ViewComponents/
â”‚   â””â”€â”€ uTProSimpleFormViewComponent.cs      # The @Component.InvokeAsync entry point
â”œâ”€â”€ Views/Partials/uTProSimpleForm/
â”‚   â”œâ”€â”€ Default.cshtml                        # Main form template
â”‚   â””â”€â”€ Fields/                               # One file per field type (+ _Default.cshtml fallback)
â””â”€â”€ wwwroot/
    â”œâ”€â”€ App_Plugins/simple-form/              # Backoffice UI
    â”‚   â”œâ”€â”€ umbraco-package.json              # Section + sidebar + dashboard + property editor + localization
    â”‚   â”œâ”€â”€ index.js                          # Dashboard shell: state, lifecycle, guards, render
    â”‚   â”œâ”€â”€ sidebar.js                        # Left Forms tree (+ / â‹¯ menu)
    â”‚   â”œâ”€â”€ bus.js                            # Event bus shared by sidebar â†” dashboard
    â”‚   â”œâ”€â”€ api.js, styles.js, date-range.js  # API helper + styles + pure date helpers
    â”‚   â”œâ”€â”€ mixins/                           # url-state / form-actions / builder / clipboard / entries
    â”‚   â”œâ”€â”€ views/                            # list / editor / entries / detail views
    â”‚   â”œâ”€â”€ property-editor/                  # form-picker.element.js + form-list-config.element.js (content data type UI + settings)
    â”‚   â””â”€â”€ lang/en-us.js                     # Localization
    â””â”€â”€ uTPro/simple-form/
        â”œâ”€â”€ css/simple-form.css               # Front-end styles
        â””â”€â”€ js/simple-form.js                 # Client-side validation & submission
```

## Static Assets

The package serves its `wwwroot` at the site root (`StaticWebAssetBasePath = /`), so:

- Backoffice assets resolve at `/App_Plugins/simple-form/...` (where Umbraco discovers the package manifest)
- Front-end assets resolve at `/uTPro/simple-form/...`

## Configuration

All settings are optional â€” the package ships with safe defaults. Options live under `uTPro:Feature:Form` in `appsettings.json` and are bound to `FormSubmissionOptions`:

| Section | Key | Default | Purpose |
|---|---|---|---|
| `uTPro:Feature:Form:RateLimit` | `Enabled` | `true` | Per-IP + per-form throttling of the public submit endpoint (`v2.3.0+`) |
| `uTPro:Feature:Form:RateLimit` | `PermitLimit` | `5` | Max submissions per window per IP + form |
| `uTPro:Feature:Form:RateLimit` | `WindowSeconds` | `60` | Fixed-window length in seconds |

See [Security & Permissions â†’ Rate limiting](security.md#rate-limiting--anti-spam-v230) for details and reverse-proxy notes.

## Uploaded files storage (`v2.1.0+`)

Files submitted through `file` fields are **not** part of `wwwroot`. They are written to
the app content root under:

```
App_Data/umbraco/Data/uTProSimpleFormUploads/{formAlias}/{yyyyMM}/{guid}{ext}
```

The root folder is configurable via `uTPro:Feature:Form:FileUploadsPath` (`v2.3.1+`) â€” absolute,
or relative to the content root â€” so multi-app deployments can share one folder. See
[Security & Permissions](security.md#storage-location-v231) for details.

This folder is never served statically. Files are only retrievable via the authenticated
`entry-file` endpoint (or bundled by the ZIP export), and are cleaned up when their entry
or form is deleted. See [Security & Permissions](security.md) for the full model.

## Database Tables

Created automatically on first startup.

**`uTProSimpleForm`** â€” form definitions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Name | nvarchar(255) | Display name |
| Alias | nvarchar(255) | Unique identifier used in code |
| FieldsJson | ntext | Legacy storage (empty for new forms) |
| GroupsJson | ntext | Groups â†’ Columns â†’ Fields as JSON |
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

**`uTProSimpleFormEntry`** â€” submissions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| FormId | int | Links to `uTProSimpleForm` |
| DataJson | ntext | Submitted data (sensitive fields encrypted; file fields store a `utpro-file:` reference, not the bytes) |
| IpAddress | nvarchar(100) | Submitter's IP |
| UserAgent | nvarchar(500) | Submitter's browser |
| CreatedUtc | datetime | Submission timestamp |

## Migration Note

Migrations run automatically on startup via Umbraco's migration system (`AsyncMigrationBase`, compatible with Umbraco 16â€“18). The plan has two steps:

1. `utprosimpleform-init` â€” creates the `uTProSimpleForm` / `uTProSimpleFormEntry` tables and seeds the sample **Contact Us** form.
2. `utprosimpleform-showinpicker` â€” ensures the `ShowInPicker` column exists (idempotent; only does work on databases created before that column).

On startup the package also ensures a **uTPro Form Picker** data type exists (created once if missing).

To re-run migrations on a fresh database, delete the row keyed `Umbraco.Core.Upgrader.State+uTPro.uTProSimpleForm` from the `umbracoKeyValue` table (the migration plan is named `uTPro.uTProSimpleForm`).
