# uTPro Form — Lightweight Form Builder for Umbraco

Create and manage dynamic forms directly from the Umbraco backoffice — no code required for everyday use. Render any form on the front-end with a single line, and extend it with custom field types in two steps.

Works with **Umbraco 16, 17 and 18** (multi-targeted `net9.0` / `net10.0`).

---

## Features

- Dedicated **uTPro Form** section in the top menu bar, with a left **Forms** tree (like Dictionary)
- Visual builder: groups → 12-column layout → fields, drag-free ordering, live settings
- **Copy / paste** groups, columns and fields via the browser's `localStorage` (works across forms, even after a page reload)
- **Import / Export** form definitions as JSON (layout only — no entries)
- **19 built-in field types** and a 2-step custom field type extension point
- Client-side validation with multi-language (Umbraco Dictionary) messages
- **Sensitive fields encrypted at rest** (ASP.NET Data Protection), masked in the UI by default
- Entry storage with search, quick **date-range filters** (Today / 7d / 30d / This month / custom), paging, CSV export
- **Shareable deep-links**: the URL reflects the current view, selected form and entry filters (survives refresh)
- **Form Picker** property editor + ready-made data type to choose a form from a Content property
- Native Umbraco **toast notifications** for all feedback
- Public REST APIs for submit / render / entries (opt-in per form)
- Role-aware UI driven by Umbraco user groups

---

## Getting Started

### Install via NuGet

```bash
dotnet add package uTPro.Feature.SimpleFormBuilder
```

### Or add as a project reference

```xml
<ProjectReference Include="path/to/uTPro.Feature.SimpleFormBuilder.csproj" />
```

On first run, uTPro Form automatically creates its database tables and seeds a sample **Contact Us** form. No manual SQL or configuration needed.

### Framework / Umbraco compatibility

| Umbraco | .NET | Package target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

The package multi-targets both, so the correct dependencies are restored automatically for your project.

---

## Where It Lives in the Backoffice

After install, a new **uTPro Form** item appears in the top (blue) section menu.

> **Access:** custom sections are governed by user-group permissions. Grant the **uTPro Form** section to a user group under **Users → User groups → _group_ → Sections → Choose → uTPro Form**, then reload the backoffice. See [Roles & Permissions](#roles--permissions).

The section uses a familiar two-pane layout:

- **Left panel (Forms tree)** — lists all forms. A **+** button creates a new form, and a **⋯ (Options)** menu offers:
  - **Reload** — refresh the list
  - **Create** — new form
  - **Import** — create a form from an exported JSON file

  Each form row also has a **⋯** menu (Edit / Entries / Export / Delete).
- **Main area** — a **Create** button, an **Import** button, a **filter** box, and the forms table (Name, Alias, Fields, Status, Actions). Selecting a form opens it; users without edit rights jump straight to its **Entries**.

---

## Rendering a Form

Drop this into any Razor view or block component:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

The `alias` matches the form you created in the **uTPro Form** section.

### Optional parameters

```razor
@await Component.InvokeAsync("uTProSimpleForm", new {
    alias = "contact-us",
    template = "MyLayout",       // use a custom Razor template
    cssClass = "my-form",        // add a CSS class to the <form> tag
    submitBtnText = "Send",      // change the submit button text
    showReset = true,            // show or hide the reset button
    resetBtnText = "Clear"       // change the reset button text
})
```

### Template resolution order

1. `Views/Partials/uTProSimpleForm/{template}.cshtml` — if a `template` parameter was passed
2. `Views/Partials/uTProSimpleForm/{alias}.cshtml` — a view named after the form alias
3. `Views/Partials/uTProSimpleForm/Default.cshtml` — the built-in default

To customize the layout for a specific form, create a file matching its alias. No config changes needed.

---

## Picking a Form from Content (Form Picker)

Instead of hard-coding the alias in a template, editors can choose a form from a Content property.

The package ships a ready-to-use **uTPro Form Picker** data type (created automatically on startup), so you can skip straight to step 2:

1. *(Optional)* Create your own: **Settings → Data Types → Create** → Property Editor **uTPro Form Picker**.
2. Add a property using the **uTPro Form Picker** data type to any Document Type (e.g. a `form` property on *Home*).
3. When editing content, pick a form from the dropdown — it stores the form's **alias**.
4. In the template, read the alias and feed it into the same View Component used for hard-coded rendering:

```razor
@{
    var formAlias = Model.Value<string>("form");
}
@if (!string.IsNullOrWhiteSpace(formAlias))
{
    @await Component.InvokeAsync("uTProSimpleForm", new { alias = formAlias })
}
```

The dropdown only lists forms whose **Show in content picker** toggle (form *Settings*) is on, so you can keep internal/system forms out of the editor's choices while they keep working everywhere else.

### Restricting a picker to specific forms (data type setting)

When you create/edit a **uTPro Form Picker** data type, its **Settings** show an **Allowed forms** list of *every* form (so you can see and tick any of them). Tick the forms this particular picker should offer:

- **Leave it empty** → default behaviour: every form with *Show in content picker* on.
- **Tick one or more forms** → the picker offers only those forms — but the *Show in content picker* rule still applies, so a ticked form appears in Content only if it also has *Show in content picker* enabled.

In other words, the Content list is always **(forms with *Show in content picker* on)**, optionally narrowed to the ticked **Allowed forms**.

### Publish validation

If a content item already stores a form that later becomes unavailable (the form was deleted, had *Show in content picker* turned off, or was removed from the picker's *Allowed forms*), the picker shows it in red as **"— not available"**. **Saving/publishing is then blocked** by a server-side validator with an inline error on the property until the editor chooses another form or clears it (**(none)**). This is enforced by `FormPickerValueValidator` (`PropertyEditors/FormPickerDataEditor.cs`), so it holds even if the value is set through the API.

---

## Import / Export

Forms can be moved between environments as JSON (definition only — **no entries, no IDs, no timestamps**).

- **Export** — from the editor toolbar, a form row, or the sidebar **⋯** menu. Downloads `{alias}.form.json`. If the open form has unsaved changes you're asked to save first.
- **Import** — from the list toolbar or sidebar **Options** menu. Always creates a **new** form; if the alias already exists it is auto-suffixed (`-copy`, `-copy-2`, …).

Requires edit permission (`canEdit`).

---

## Copy / Paste (Groups, Columns, Fields)

The builder can copy a whole **group**, **column** or **field** and paste it elsewhere — even into a different form.

- Copy buttons sit on each group/column/field; **Paste** buttons appear only when the clipboard holds a matching item.
- Backed by the browser's **`localStorage`**, so the copied item survives navigating between forms and even a full page reload. Copying a different item type swaps what the Paste buttons offer.
- On paste, all internal IDs are regenerated and colliding field `name`s are de-duped (`_copy`, `_copy2`, …) so submissions never clash.

1. Open the **uTPro Form** section
2. Click **Create** (main area) or **+** (left panel)
3. Give it a **Name** and **Alias** (the alias is what you use in code)
4. Add **Groups** to organize fields into sections
5. Inside each group, add **Columns** (12-column grid) and drop **Fields** into them
6. Configure each field: type, label, placeholder, validation, etc.
7. Set the **Success Message**, optional **Redirect URL**, and **Email Notification**
8. Save

Your form is ready to render on the front-end.

---

## Built-in Field Types

Ships with 19 field types out of the box:

| Type | Description | Dedicated partial? |
|---|---|---|
| `text` | Single-line text input | No (uses `_Default.cshtml`) |
| `email` | Email input | No |
| `tel` | Phone number | No |
| `number` | Numeric input | No |
| `url` | URL input | No |
| `password` | Password input (auto-encrypted at rest) | No |
| `date` | Date picker | No |
| `file` | File upload | No |
| `textarea` | Multi-line text | Yes |
| `select` | Dropdown menu | Yes |
| `checkbox` | Single or multi-checkbox | Yes |
| `radio` | Radio button group | Yes |
| `hidden` | Hidden field | Yes |
| `accept` | Terms & conditions checkbox with link | Yes |
| `range` | Slider with min/max/step | Yes |
| `color` | Color picker | Yes |
| `time` | Time picker with min/max | Yes |
| `div` | HTML content block (not an input) | Yes |
| `step` | Visual step divider | Yes |

Types without a dedicated partial fall back to `_Default.cshtml`, which renders a standard `<input>`.

---

## Adding a Custom Field Type

It takes two steps.

### Step 1 — Create a Razor partial

Create a `.cshtml` file named after your field type:

```
Views/Partials/uTProSimpleForm/Fields/{yourType}.cshtml
```

Minimal template:

```razor
@using uTPro.Feature.SimpleFormBuilder.Helpers
@model uTPro.Feature.SimpleFormBuilder.Models.FormFieldViewModel
@{ var h = new FieldHelper(Model, ViewData); }

@h.Label()
<input type="text" id="@h.FieldId" name="@h.Name"
       placeholder="@Model.Placeholder"
       value="@Model.DefaultValue"
       @h.RequiredAttr()
       @h.DataMsgAttr() />
@h.Error()
```

uTPro Form auto-detects the new file. No changes to `Default.cshtml` or any config.

### Step 2 — Register it in the backoffice picker

You **don't** edit the package. From your own site, register the type in a composer:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.SimpleFormBuilder.Services;

public class MyFormFieldsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AdduTProSimpleFormFieldType("yourType", "Your Type Label");
}
```

Register several at once with `builder.AdduTProSimpleFormFieldTypes(...)`. Build and restart. Your new field type now appears in the form builder, merged with the built-in ones.

---

## FieldHelper — Toolkit for Field Partials

Every field partial can use `FieldHelper` to avoid repetitive HTML:

```razor
@{ var h = new FieldHelper(Model, ViewData); }
```

| Call | Renders |
|---|---|
| `h.FieldId` | Unique HTML id like `sf-contact-us-email` |
| `h.Name` | The field name for form submission |
| `h.Label()` | `<label>` with a red asterisk if required |
| `h.LabelNoFor()` | Label without `for` (for checkbox/radio groups) |
| `h.Error()` | `<span class="sf-error">` for validation messages |
| `h.RequiredAttr()` | Outputs `required` or nothing |
| `h.PatternAttr()` | Outputs `pattern="..."` or nothing |
| `h.DataMsgAttr()` | Outputs `data-msg="..."` with auto-translated dictionary keys |
| `h.Attr("key", "default")` | Reads from `Field.Attributes` with a fallback |
| `h.OptionalAttr("min", val)` | Outputs `min="5"` only if `val` is not empty |

### Multi-language validation messages

Wrap an Umbraco Dictionary key in double curly braces:

```
{{SimpleForm.NameRequired}}
```

`FieldHelper` resolves it to the current culture at render time.

---

## Full Example: Star Rating Field

**Step 1** — Create `Views/Partials/uTProSimpleForm/Fields/star-rating.cshtml`:

```razor
@using uTPro.Feature.SimpleFormBuilder.Helpers
@model uTPro.Feature.SimpleFormBuilder.Models.FormFieldViewModel
@{
    var h = new FieldHelper(Model, ViewData);
    var max = h.Attr("max", "5");
}

@h.Label()
<div class="sf-star-rating">
    @for (var i = 1; i <= int.Parse(max); i++)
    {
        <label>
            <input type="radio" name="@h.Name" value="@i"
                   @h.RequiredAttr() @h.DataMsgAttr() />
            ⭐
        </label>
    }
</div>
@h.Error()
```

**Step 2** — Register the type from your site in a composer (no package edits):

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.SimpleFormBuilder.Services;

public class StarRatingFieldComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AdduTProSimpleFormFieldType("star-rating", "Star Rating");
}
```

Done. The field shows up in the builder and renders with stars on the front-end.

> A complete, runnable version of this example ships in the bundled **TestSite**
> (`Examples/StarRatingFieldComposer.cs` + `Views/Partials/uTProSimpleForm/Fields/star-rating.cshtml`),
> demonstrating exactly how a NuGet consumer extends the package from their own project.

---

## Overriding Views (NuGet Users)

When installed via NuGet, all Razor views are compiled into the package DLL. To customize any view, create a file at the **same path** in your web project:

```
YourWebProject/
  Views/Partials/uTProSimpleForm/
    Default.cshtml                  ← overrides the form layout
    Fields/
      textarea.cshtml               ← overrides just the textarea field
      star-rating.cshtml            ← adds a brand new field type
```

ASP.NET Core picks up local files over the ones in the package. No configuration needed.

---

## JavaScript Hooks

Two front-end hooks for custom client-side logic:

```javascript
// Runs before submission. Return false to cancel, or an object to merge extra data.
window.__sfBeforeSubmit = async function (alias, data, formElement) {
    if (alias === 'contact-us') {
        data.source = 'homepage';
    }
    return data;
};

// Runs after a successful submission.
window.__sfAfterSubmit = function (alias, success, result) {
    console.log('Submitted:', alias, result.message);
};
```

---

## Public APIs

REST endpoints for headless or hybrid use cases. These are **anonymous** and bypass backoffice roles — enable them deliberately.

### Submit a form (always available)

```http
POST /api/utpro/simple-form/submit
Content-Type: application/json

{
  "alias": "contact-us",
  "data": { "name": "Jane", "email": "jane@example.com", "message": "Hello!" }
}
```

### Get form definition (opt-in per form: *Enable Render API*)

```http
GET /api/utpro/simple-form/render/{alias}
```

### Get form entries (opt-in per form: *Enable Entries API*, sensitive data always masked)

```http
GET /api/utpro/simple-form/entries/{alias}?skip=0&take=20
```

> ⚠️ Enabling the Entries API exposes that form's submissions (sensitive fields masked) to anyone who knows the alias. Use with care.

---

## Roles & Permissions

Two things govern access to the backoffice UI:

1. **Section visibility** — a user sees the **uTPro Form** menu only if their user group is granted that section (independent of any other section).
2. **Action permissions** — returned by the API for the current user:
   - `isAdmin` — member of the built-in **Administrators** group.
   - `canEdit` (manage forms) — **admin _or_ the user's group has the _Settings_ section**.
   - `canViewSensitive` — **admin _or_ member of a group whose alias is `sensitiveData`**.

| Capability | Required permission |
|---|---|
| See the **uTPro Form** menu | Group granted the *uTPro Form* section |
| View form list, view entries, export CSV | Any backoffice user with the section |
| Create / edit / delete forms | `canEdit` (admin or Settings access) |
| Delete entry / bulk delete | `canEdit` |
| See decrypted sensitive/password values (else `*****`) | Admin or `sensitiveData` group |

> The backoffice API requires a valid backoffice login; write actions additionally require `canEdit`. The API is **not** gated by the section — the section grant only controls UI visibility.

### Test Accounts (TestSite)

The bundled `TestSite` auto-seeds the accounts below on startup (see
`TestUserSeeder.cs`) so the role/permission matrix can be exercised immediately —
even after wiping the database. All share the unattended admin password
`Admin1234!`. The seeder also creates the `sensitiveData` and `Admin Custom`
user groups and grants them the *uTPro Form* section.

| Email | Group(s) | Behaviour in uTPro Form |
|---|---|---|
| `admin@example.com` | Administrators | Everything: design forms, manage entries, view sensitive data |
| `editor@example.com` | Editor *(+ uTPro Form section)* | View forms & entries, export CSV; **cannot** design/delete; sensitive shown as `*****` |
| `editorSD@example.com` | Editor + `sensitiveData` *(+ uTPro Form section)* | Same as editor, **plus** can view decrypted sensitive values |
| `adminCustom@example.com` | Admin Custom — clone of Administrators (sections incl. **Settings** + uTPro Form) | **Can design/edit/delete forms** (has Settings ⇒ `canEdit`), but sensitive values stay masked (not admin, not `sensitiveData`) |

> **Key rule:** form management (`canEdit`) is granted by the **Settings** section, not by the Administrators group alone. Sensitive-data viewing is a separate lever, granted only by the Administrators group or the `sensitiveData` group.

> The seeder is **TestSite-only** scaffolding — it is not part of the shipped package. In a real site you create users/groups through the backoffice as usual.

---

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

---

## Static Assets

The package serves its `wwwroot` at the site root (`StaticWebAssetBasePath = /`), so:

- Backoffice assets resolve at `/App_Plugins/simple-form/...` (where Umbraco discovers the package manifest)
- Front-end assets resolve at `/uTPro/simple-form/...`

---

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

---

## Security

- **Sensitive fields** (password type or fields marked sensitive) are encrypted via ASP.NET Data Protection before storage and masked in the UI by default.
- **Form management** (create/edit/delete, delete entries) requires `canEdit` (admin or Settings access).
- **Sensitive data viewing** requires admin or the `sensitiveData` group.
- **Public APIs** are disabled by default per form and must be explicitly enabled.

---

## Migration Note

Migrations run automatically on startup via Umbraco's migration system (`AsyncMigrationBase`, compatible with Umbraco 16–18). The plan has two steps:

1. `utprosimpleform-init` — creates the `uTProSimpleForm` / `uTProSimpleFormEntry` tables and seeds the sample **Contact Us** form.
2. `utprosimpleform-showinpicker` — ensures the `ShowInPicker` column exists (idempotent; only does work on databases created before that column).

On startup the package also ensures a **uTPro Form Picker** data type exists (created once if missing).

To re-run migrations on a fresh database, delete the row keyed
`Umbraco.Core.Upgrader.State+uTPro.uTProSimpleForm` from the `umbracoKeyValue` table
(the migration plan is named `uTPro.uTProSimpleForm`).
