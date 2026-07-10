# uTPro Simple Form Builder for Umbraco

A lightweight form builder — create and manage dynamic forms directly from the Umbraco backoffice with no code required for everyday use. Render any form on the front-end with a single line, and extend it with custom field types without touching the package.

Works with **Umbraco 16, 17 and 18** (multi-targeted `net9.0` / `net10.0`).

Database-agnostic: runs on **SQL Server**, **SQLite** and **PostgreSQL** (`v2.0.0+`).

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)
[![Umbraco 16+](https://img.shields.io/badge/Umbraco-16%2B-3544B1)](https://umbraco.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

![uTPro Form builder](https://raw.githubusercontent.com/T4VN/uTPro.Feature.SimpleFormBuilder/main/Image/Screenshots/form-builder.png)

---

## Features

- Dedicated **uTPro Form** section with a left **Forms** tree (like Dictionary)
- Visual builder: groups → 12-column layout → fields, with live settings
- **Copy / paste** groups, columns and fields across forms (browser `localStorage`)
- **Import / Export** form definitions as JSON (layout only — no entries)
- **19 built-in field types** + a custom field type extension point (custom types can declare their own labelled settings)
- Client-side validation with multi-language (Umbraco Dictionary) messages
- **Sensitive fields encrypted at rest** (ASP.NET Data Protection), masked in the UI
- **File uploads** stored outside `wwwroot`, served only via an authenticated download endpoint (`v2.1.0+`); storage location configurable via `uTPro:Feature:Form:FileUploadsPath` for shared/load-balanced deployments (`v2.3.1+`)
- Entry storage with search, date-range filters, paging, and export as **CSV** (data only) or **ZIP** (per-entry folders with data + uploaded files)
- **Form Picker** property editor (+ ready-made data type) to choose a form from content, with server-side publish validation
- Public REST APIs for submit / render / entries (opt-in per form)
- **Anti-spam & rate limiting** — built-in per-IP + per-form rate limit on the public submit endpoint (configurable), plus a pluggable **submission pipeline** (`IFormSubmissionHandler`) for captcha verification, custom validation, or your own gatekeepers (`v2.3.0+`)
- Role-aware UI driven by Umbraco user groups

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
| [Public APIs & Import/Export](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/public-apis.md) | submit / render / entries endpoints, submission pipeline (`IFormSubmissionHandler`), JSON import/export |
| [Security & Permissions](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/security.md) | Roles, sensitive-data encryption (encode/decode), rate limiting, test accounts |
| [Reference](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/reference.md) | Project structure, static assets, database tables, migrations |

---

## License & Author

MIT © [T4VN](https://github.com/T4VN). Issues and contributions welcome on the
[GitHub repository](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder).
