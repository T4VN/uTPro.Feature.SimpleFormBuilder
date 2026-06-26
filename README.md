# SimpleForm — Lightweight Form Builder for Umbraco

SimpleForm lets you create and manage dynamic forms from the Umbraco backoffice — no code required for everyday use. For developers, adding a custom field type takes just two steps.

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

That's it. On first run, SimpleForm automatically creates its database tables and seeds a sample "Contact Us" form.

---

## Rendering a Form

Drop this line into any Razor view or block component:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

The `alias` matches the form you created in the backoffice (the **uTPro Form** section).

### Optional Parameters

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

### How Template Resolution Works

SimpleForm looks for a matching view in this order:

1. `Views/Partials/uTProSimpleForm/{template}.cshtml` — if you passed a `template` parameter
2. `Views/Partials/uTProSimpleForm/{alias}.cshtml` — a view named after the form alias
3. `Views/Partials/uTProSimpleForm/Default.cshtml` — the built-in default

To customize the layout for a specific form, just create a file matching its alias. No config changes needed.

---

## Creating Forms in the Backoffice

1. Go to the **uTPro Form** section in the Umbraco backoffice (top menu bar)
2. Click **New Form**
3. Give it a **Name** and **Alias** (the alias is what you use in code)
4. Add **Groups** to organize your fields into sections
5. Inside each group, add **Columns** (based on a 12-column grid) and drop **Fields** into them
6. Configure each field: type, label, placeholder, validation, etc.
7. Set up the **Success Message**, optional **Redirect URL**, and **Email Notification**
8. Save

Your form is ready to render on the frontend.

---

## Built-in Field Types

SimpleForm ships with 19 field types out of the box:

| Type | Description | Has its own partial? |
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
| `div` | HTML content block (not a form input) | Yes |
| `step` | Visual step divider | Yes |

Types without a dedicated partial fall back to `_Default.cshtml`, which renders a standard `<input>` element.

---

## Adding a Custom Field Type

This is the part most developers care about. It takes two steps.

### Step 1 — Create a Razor partial

Create a `.cshtml` file named after your field type:

```
Views/Partials/uTProSimpleForm/Fields/{yourType}.cshtml
```

Here's a minimal template:

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

SimpleForm auto-detects the new file. No changes to `Default.cshtml` or any config.

### Step 2 — Register it in the backoffice picker

Open `Controllers/uTProSimpleFormApiController.cs` and add one line to the `FieldTypes()` method:

```csharp
new { type = "yourType", label = "Your Type Label" },
```

Build. Your new field type now appears in the backoffice form builder.

---

## FieldHelper — The Toolkit for Field Partials

Every field partial can use `FieldHelper` to avoid repetitive HTML boilerplate:

```razor
@{ var h = new FieldHelper(Model, ViewData); }
```

| What you call | What it renders |
|---|---|
| `h.FieldId` | Unique HTML id like `sf-contact-us-email` |
| `h.Name` | The field name for form submission |
| `h.Label()` | `<label>` with a red asterisk if required |
| `h.LabelNoFor()` | Same label but without `for` attribute (for checkbox/radio groups) |
| `h.Error()` | `<span class="sf-error">` for client-side validation messages |
| `h.RequiredAttr()` | Outputs `required` or nothing |
| `h.PatternAttr()` | Outputs `pattern="..."` or nothing |
| `h.DataMsgAttr()` | Outputs `data-msg="..."` with auto-translated dictionary keys |
| `h.Attr("key", "default")` | Reads a value from `Field.Attributes` with a fallback |
| `h.OptionalAttr("min", val)` | Outputs `min="5"` only if `val` is not empty |

### Multi-language Support

Validation messages support Umbraco Dictionary keys. Wrap the key in double curly braces:

```
{{SimpleForm.NameRequired}}
```

`FieldHelper` automatically resolves it to the current culture's translation at render time.

---

## Full Example: Star Rating Field

Here's a complete walkthrough of adding a custom "star rating" field.

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

**Step 2** — Add to `uTProSimpleFormApiController.cs`:

```csharp
new { type = "star-rating", label = "Star Rating" },
```

Done. The field shows up in the backoffice builder, and the frontend renders it with stars.

---

## Overriding Views (NuGet Users)

When you install SimpleForm via NuGet, all Razor views are compiled into the package DLL. To customize any view, create a file at the **same path** in your web project:

```
YourWebProject/
  Views/Partials/uTProSimpleForm/
    Default.cshtml                  ← overrides the form layout
    Fields/
      textarea.cshtml               ← overrides just the textarea field
      star-rating.cshtml            ← adds a brand new field type
```

ASP.NET Core automatically picks up local files over the ones in the NuGet package. No configuration needed.

Static assets (CSS, JS) are served from `/_content/uTPro.Feature.SimpleFormBuilder/...` and can be overridden by adding your own stylesheet.

---

## JavaScript Hooks

SimpleForm exposes two hooks for custom client-side logic:

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

SimpleForm includes REST endpoints for headless or hybrid use cases.

### Submit a form

```http
POST /api/utpro/simple-form/submit
Content-Type: application/json

{
  "alias": "contact-us",
  "data": {
    "name": "Jane",
    "email": "jane@example.com",
    "message": "Hello!"
  }
}
```

### Get form definition (must be enabled per form)

```http
GET /api/utpro/simple-form/render/{alias}
```

### Get form entries (must be enabled per form, sensitive data is masked)

```http
GET /api/utpro/simple-form/entries/{alias}?skip=0&take=20
```

---

## Project Structure

```
uTPro.Feature.SimpleFormBuilder/
├── Controllers/
│   ├── uTProSimpleFormApiController.cs      # Backoffice API (CRUD, entries, field types)
│   └── uTProSimpleFormSubmitController.cs   # Public API (submit, render, entries)
├── Helpers/
│   ├── FieldPartialResolver.cs         # Finds the right partial for each field type
│   ├── uTProSimpleFormAssets.cs             # Resolves CSS/JS paths (local vs NuGet)
│   └── uTProSimpleFormHtmlHelper.cs         # FieldHelper class used in partials
├── Migrations/
│   └── uTProSimpleFormMigration.cs          # Creates tables + seeds sample data
├── Models/
│   └── FormModels.cs                   # All DTOs, ViewModels, and request models
├── Services/
│   └── uTProSimpleFormService.cs            # Core logic, encryption, entry management
├── ViewComponents/
│   └── uTProSimpleFormViewComponent.cs      # The @Component.InvokeAsync entry point
├── Views/Partials/uTProSimpleForm/
│   ├── Default.cshtml                  # Main form template
│   └── Fields/                         # One file per field type
│       ├── _Default.cshtml             # Fallback for standard inputs
│       ├── textarea.cshtml
│       ├── select.cshtml
│       └── ...
└── wwwroot/
    ├── App_Plugins/simple-form/        # Backoffice dashboard UI
    └── uTPro/simple-form/
        ├── css/simple-form.css         # Frontend styles
        └── js/simple-form.js           # Client-side validation & submission
```

---

## Database Tables

Created automatically on first startup. No manual SQL needed.

**`utpro_uTProSimpleForm`** — stores form definitions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Name | nvarchar(255) | Display name |
| Alias | nvarchar(255) | Unique identifier used in code |
| FieldsJson | ntext | Legacy storage (empty for new forms) |
| GroupsJson | ntext | Groups → Columns → Fields as JSON |
| SuccessMessage | nvarchar(1000) | Shown after successful submission |
| RedirectUrl | nvarchar(500) | Optional redirect after submission |
| EmailTo | nvarchar(500) | Notification email recipient |
| EmailSubject | nvarchar(500) | Notification email subject |
| StoreEntries | bit | Whether to save submissions |
| IsEnabled | bit | Active or disabled |
| VisibleColumnsJson | ntext | Backoffice entry table config |
| EnableRenderApi | bit | Allow public form definition API |
| EnableEntriesApi | bit | Allow public entries API |
| CreatedUtc | datetime | Created timestamp |
| UpdatedUtc | datetime | Last modified timestamp |

**`utpro_uTProSimpleFormEntry`** — stores form submissions

| Column | Type | Purpose |
|---|---|---|
| Id | int (PK) | Auto-increment |
| FormId | int | Links to utpro_uTProSimpleForm |
| DataJson | ntext | Submitted data (sensitive fields encrypted) |
| IpAddress | nvarchar(100) | Submitter's IP address |
| UserAgent | nvarchar(500) | Submitter's browser |
| CreatedUtc | datetime | Submission timestamp |

---

## Security

- **Sensitive fields** (password type or fields marked as sensitive) are encrypted using ASP.NET Data Protection before storage
- **Backoffice access** — only administrators can create, edit, or delete forms
- **Sensitive data viewing** — masked by default; only users with the `sensitiveData` group or admin role can see decrypted values
- **Entry deletion** — admin only
- **Public APIs** — disabled by default per form; must be explicitly enabled

---

## Roles & Permissions

The backoffice UI lives in its own top-level **uTPro Form** section. Two things govern access:

1. **Section visibility** — a user only sees the **uTPro Form** menu item if their User Group is granted that section (Users → User groups → *group* → Sections → Choose → *uTPro Form*). This is independent of the Settings section.
2. **Action permissions** — driven by the user, not by the section:
   - `isAdmin` = member of the **Administrators** group.
   - `canViewSensitive` = admin **or** member of a group whose alias is `sensitiveData`.

| Capability | Required permission |
|---|---|
| See the **uTPro Form** menu | Group granted the *uTPro Form* section |
| View form list, view entries, view entry detail | Any backoffice user with the section |
| Export entries to CSV | Any user who can view entries (not admin-only) |
| Create / edit / delete forms (design) | **Admin** |
| Delete entry / bulk delete | **Admin** |
| See decrypted sensitive/password values (others see `*****`) | **Admin** or `sensitiveData` group |

> **Note:** Having the *Settings* section does **not** grant form-design rights. Designing forms requires the Administrators group. The API endpoints require a valid backoffice login (write actions additionally require admin), but they are not gated by the section — the section grant only controls UI visibility.

### Test Accounts (TestSite)

The bundled `uTPro.Feature.SimpleFormBuilder.TestSite` ships with an unattended admin. To exercise the role matrix above, create these users (or use them if already seeded). All passwords are the **same as the admin account** (`Admin1234!` in the TestSite).

| Email | Group(s) | What they can do |
|---|---|---|
| `admin@example.com` | Administrators | Everything: design forms, manage entries, view sensitive data |
| `editor@example.com` | Editor *(+ uTPro Form section)* | View forms & entries, export CSV; **cannot** design forms or delete; sensitive values shown as `*****` |
| `editorSD@example.com` | Editor + `sensitiveData` *(+ uTPro Form section)* | Same as editor, **plus** can view decrypted sensitive/password values |
| `adminCustom@example.com` | Admin Custom — a clone of Administrators with custom sections (Content, Media, Library, Settings, Users, Members, Translation, uTPro Form) | Sees the **uTPro Form** menu and can view forms & entries + export CSV, but **cannot** design forms, delete, or view sensitive data |

> **Important — broad section access is not the same as admin rights.** `adminCustom` can open almost every section, yet inside uTPro Form it behaves like an editor. The package gates design/delete/sensitive-data on membership of the built-in **Administrators** group (`user.IsAdmin()`), **not** on how many sections a group has. A custom group cloned from Administrators uses a different alias, so `IsAdmin()` returns `false` for it.

> To set up `editorSD`, create a User Group with the alias `sensitiveData`, grant it the *uTPro Form* section, and add the user to both the Editor and `sensitiveData` groups.

---

## Migration Note

The database migration runs automatically on application startup via Umbraco's migration system. If you need to re-run it on a fresh database, delete the `uTPro.uTProSimpleForm` key from the `umbracoKeyValue` table.
