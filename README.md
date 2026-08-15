# uTPro Simple Form Builder for Umbraco

A lightweight form builder — create and manage dynamic forms directly from the Umbraco backoffice with no code required for everyday use.

Works with **Umbraco 16, 17 and 18**. Database-agnostic: **SQL Server**, **SQLite** and **PostgreSQL**.

[![NuGet](https://img.shields.io/nuget/v/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![NuGet Downloads](https://img.shields.io/nuget/dt/uTPro.Feature.SimpleFormBuilder.svg)](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder)
[![Umbraco Marketplace](https://img.shields.io/badge/Umbraco-Marketplace-blue)](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)
[![Umbraco 16+](https://img.shields.io/badge/Umbraco-16%2B-3544B1)](https://umbraco.com)
[![License: Free (proprietary)](https://img.shields.io/badge/License-Free%20(proprietary)-green.svg)](LICENSE.txt)

![uTPro Form builder](https://raw.githubusercontent.com/T4VN/uTPro.Feature.SimpleFormBuilder/main/Image/Screenshots/form-builder.png)

---

## Features

- Dedicated **uTPro Form** section with a left **Forms** tree
- Visual builder: groups → 12-column layout → fields, with live settings
- **Conditional field visibility** — show or hide fields based on another field's value
- **Copy / paste** groups, columns and fields across forms
- **Import / Export** form definitions as JSON
- **19 built-in field types** + custom field type extension point
- Client-side + server-side validation with multi-language messages
- **Sensitive fields encrypted at rest**, masked in the UI
- **File uploads** stored outside `wwwroot`, served via authenticated endpoint
- Entry storage with search, date-range filters, paging, CSV/ZIP export
- **Form Picker** property editor for content
- Public REST APIs for submit / render / entries
- **Anti-spam & rate limiting** + pluggable submission pipeline (`IFormSubmissionHandler`)

---

## Quick Start

```bash
dotnet add package uTPro.Feature.SimpleFormBuilder
```

Render a form anywhere:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

| Umbraco | .NET | Target |
|---|---|---|
| 16 | .NET 9 | `net9.0` |
| 17 & 18 | .NET 10 | `net10.0` |

---

## Configuration

Under `uTPro:Feature:Form` in `appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `RateLimit:Enabled` | `true` | Enable per-IP + per-form rate limit |
| `RateLimit:PermitLimit` | `5` | Max submissions per window |
| `RateLimit:WindowSeconds` | `60` | Rate limit window |
| `FileUploadsPath` | `""` | Custom file upload folder |
| `MaxExportEntries` | `10000` | Cap for ZIP export |

---

## 📖 Full Documentation

**[docs.utpro.dev/uTPro.Feature.SimpleFormBuilder](https://docs.utpro.dev/uTPro.Feature.SimpleFormBuilder/)**

---

## License

Free to use (including commercially) under a proprietary [End User License Agreement](LICENSE.txt). The package ships as a compiled NuGet package; the source is not published, and modifying, reverse engineering, or redistributing the package is not permitted.

---

> 📦 [NuGet](https://www.nuget.org/packages/uTPro.Feature.SimpleFormBuilder) · [GitHub](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder) · [Umbraco Marketplace](https://marketplace.umbraco.com/package/utpro.feature.simpleformbuilder)
