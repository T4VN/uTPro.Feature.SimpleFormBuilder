# uTPro Simple Form Builder for Umbraco

**Documentation:** <https://docs.utpro.dev/uTPro.Feature.SimpleFormBuilder/>

A lightweight form builder — create and manage dynamic forms directly from the Umbraco backoffice with no code required for everyday use. Render any form on the front-end with a single line, and extend it with custom field types without touching the package.

Works with **Umbraco 16, 17 and 18** (multi-targeted `net9.0` / `net10.0`).

Database-agnostic: runs on **SQL Server**, **SQLite** and **PostgreSQL** (`v2.0.0+`).

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)
[![Umbraco 16+](https://img.shields.io/badge/Umbraco-16%2B-3544B1)](https://umbraco.com)
[![License: Free (proprietary)](https://img.shields.io/badge/License-Free%20(proprietary)-green.svg)](LICENSE.txt)

![uTPro Form builder](https://raw.githubusercontent.com/T4VN/uTPro.Feature.SimpleFormBuilder/main/Image/Screenshots/form-builder.png)

---

## Features

- Dedicated **uTPro Form** section with a left **Forms** tree (like Dictionary)
- Visual builder: groups → 12-column layout → fields, with live settings
- **Conditional field visibility** (`v5.0.0+`) — show or hide fields dynamically based on another field's value (e.g. a Dropdown selection). Configure conditions per-field in the Field Settings dialog with Show/Hide action, all/any match logic, and 10 comparison operators. When the source field has predefined options, values are selectable from a dropdown
- **Copy / paste** groups, columns and fields across forms (browser `localStorage`)
- **Import / Export** form definitions as JSON (layout only — no entries)
- **19 built-in field types** + a custom field type extension point (custom types can declare their own labelled settings)
- Client-side validation with multi-language (Umbraco Dictionary) messages, backed by **server-side validation** on submit (field type / pattern / length) plus a **mass-assignment guard** so only declared form fields are stored
- **Sensitive fields encrypted at rest** (ASP.NET Data Protection), masked in the UI
- **Secret field attributes redacted** from public output — secrets such as a Cloudflare Turnstile `secretKey` are stripped from the public render API and the backoffice list/get responses, so only public attributes (e.g. `siteKey`) are exposed
- **File uploads** stored outside `wwwroot`, served only via an authenticated download endpoint (`v2.1.0+`); storage location configurable via `uTPro:Feature:Form:FileUploadsPath` for shared/load-balanced deployments (`v2.3.1+`). File-upload fields without an explicit `accept` list fall back to a safe deny-list (executable/script/svg/html extensions rejected) and a default max size
- Entry storage with search, date-range filters, paging, and export as **CSV** (data only) or **ZIP** (per-entry folders with data + uploaded files); CSV and ZIP exports **neutralize spreadsheet formula-injection** (cells starting with `=`, `+`, `-`, `@`, tab or CR are quoted as text)
- **Consistent entry counts** (`v5.1.0+`) — the entries toolbar badge always reports the form's all-time count (matching the edit screen) while the table honours the active search/date filters; a new **All time** quick-range option lists every submission in one click. The entries APIs return `totalAll` alongside `total`
- **Form Picker** property editor (+ ready-made data type) to choose a form from content, with server-side publish validation
- Public REST APIs for submit / render / entries (opt-in per form)
- **Anti-spam & rate limiting** — built-in per-IP + per-form rate limit on the public submit endpoint (configurable), plus a pluggable **submission pipeline** (`IFormSubmissionHandler`) for captcha verification, custom validation, or your own gatekeepers (`v2.3.0+`)
- Backoffice read endpoints (forms / entries / files / export) require **forms-management** (Settings-section) permission
- Role-aware UI driven by Umbraco user groups

---

## What's New in v5.0.0

**Conditional Field Visibility** — fields can now show or hide themselves at runtime based on the value of other form fields.

Open any field's **Field Settings** dialog and scroll to the new **Conditions** section:

1. Toggle **On** to enable conditions
2. Choose the action: **Show** or **Hide** this field
3. Choose the match logic: **all** (every rule must match) or **any** (at least one rule matches)
4. Add one or more rules — each rule references another field, a comparison operator, and a target value

**Operators available:** is, is not, contains, does not contain, starts with, ends with, is greater than, is less than, is empty, is not empty.

When the referenced field has predefined options (Dropdown, Radio, Checkbox), the value picker renders as a **dropdown** populated with those options — no manual typing needed.

**Runtime behaviour:** conditions are evaluated client-side in real time as the user fills in the form. Fields hidden by conditions are excluded from validation and are not submitted. No database migration is required — conditions are stored inside the existing JSON column.

---

## Quick Start

Install:

```bash
dotnet add package uTPro.Feature.SimpleFormBuilder
```

On first run it creates its tables and seeds a sample **Contact Us** form. Grant the **uTPro Form** section to your user group, build a form, then render it anywhere:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

| Umbraco | .NET | Target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

---

## Configuration

Most settings are managed in the backoffice, but a few live under `uTPro:Feature:Form` in `appsettings.json`:

- **`FileUploadsPath`** (string, optional): folder for stored upload files, kept outside `wwwroot`. A relative path resolves under `ContentRoot`; leaving it empty falls back to `<ContentRoot>/umbraco/Data/uTProSimpleFormUploads`. Point it at shared storage for load-balanced / multi-node deployments.
- **`MaxExportEntries`** (int, default `10000`): caps how many entries a single ZIP export materialises, bounding memory use. Values `<= 0` fall back to the default.

```json
{
  "uTPro": {
    "Feature": {
      "Form": {
        "RateLimit": {
          "Enabled": true,
          "PermitLimit": 5,
          "WindowSeconds": 60
        },
        "FileUploadsPath": "",
        "MaxExportEntries": 10000
      }
    }
  }
}
```

See [Security & Permissions](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/security.md) for the full rate-limit options.

---

## Database support

Runs on **SQL Server**, **SQLite** and **PostgreSQL**. All data access uses NPoco strongly-typed queries and provider-agnostic migrations (large JSON columns use `SpecialDbTypes.NVARCHARMAX`, which maps to `nvarchar(max)` on SQL Server and `text` on SQLite / PostgreSQL), so table/column identifiers and types are handled correctly on every database.

For **PostgreSQL**, install the community provider [`Our.Umbraco.PostgreSql`](https://github.com/idseefeld/PostgreSqlForUmbraco), enable it in `Program.cs` with `.AddUmbracoPostgreSqlSupport()`, and set provider name `Npgsql2` in the connection string. The form builder then runs with full functionality — verified end-to-end (unattended install, migration, seed, form submit, and encrypted sensitive fields):

![uTPro Simple Form Builder running on PostgreSQL](https://raw.githubusercontent.com/T4VN/uTPro.Feature.SimpleFormBuilder/main/Image/Screenshots/postgresql.png)

---

## Documentation

| Guide | What's inside |
|---|---|
| [Getting Started](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/getting-started.md) | Install, compatibility, backoffice layout, building a form, copy/paste |
| [Rendering a Form](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/rendering.md) | ViewComponent, parameters, template resolution, overriding views, JS hooks |
| [Form Picker](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/form-picker.md) | Choose a form from content, Allowed-forms setting, publish validation |
| [Field Types](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/field-types.md) | Built-in types, custom field types + custom settings, FieldHelper, Star Rating example |
| [Conditions](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/conditions.md) | Show/hide fields based on another field's value, operators, runtime behaviour |
| [Public APIs & Import/Export](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/public-apis.md) | submit / render / entries endpoints, submission pipeline (`IFormSubmissionHandler`), JSON import/export |
| [Security & Permissions](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/security.md) | Roles, sensitive-data encryption (encode/decode), rate limiting, test accounts |
| [Reference](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/reference.md) | Project structure, static assets, database tables, migrations |

---

## License & Author

By [T4VN](https://github.com/T4VN). Free to use — including in commercial projects — under a proprietary [End User License Agreement](LICENSE.txt). The package ships as a compiled NuGet package; the source is not published, and modifying, reverse engineering, or redistributing the package is not permitted. See [LICENSE.txt](LICENSE.txt) for full terms. Issues welcome on the [GitHub repository](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder).
