# uTPro Form — Lightweight Form Builder for Umbraco

Create and manage dynamic forms directly from the Umbraco backoffice — no code required for everyday use. Render any form on the front-end with a single line, and extend it with custom field types without touching the package.

Works with **Umbraco 16, 17 and 18** (multi-targeted `net9.0` / `net10.0`).

---

## Features

- Dedicated **uTPro Form** section with a left **Forms** tree (like Dictionary)
- Visual builder: groups → 12-column layout → fields, with live settings
- **Copy / paste** groups, columns and fields across forms (browser `localStorage`)
- **Import / Export** form definitions as JSON (layout only — no entries)
- **19 built-in field types** + a 2-step custom field type extension point
- Client-side validation with multi-language (Umbraco Dictionary) messages
- **Sensitive fields encrypted at rest** (ASP.NET Data Protection), masked in the UI
- Entry storage with search, date-range filters, paging and CSV export
- **Form Picker** property editor (+ ready-made data type) to choose a form from content, with server-side publish validation
- Public REST APIs for submit / render / entries (opt-in per form)
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

## Documentation

| Guide | What's inside |
|---|---|
| [Getting Started](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/getting-started.md) | Install, compatibility, backoffice layout, building a form, copy/paste |
| [Rendering a Form](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/rendering.md) | ViewComponent, parameters, template resolution, overriding views, JS hooks |
| [Form Picker](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/form-picker.md) | Choose a form from content, Allowed-forms setting, publish validation |
| [Field Types](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/field-types.md) | Built-in types, custom field types, FieldHelper, Star Rating example |
| [Public APIs & Import/Export](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/public-apis.md) | submit / render / entries endpoints, JSON import/export |
| [Security & Permissions](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/security.md) | Roles, sensitive-data encryption (encode/decode), test accounts |
| [Reference](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder/blob/main/docs/reference.md) | Project structure, static assets, database tables, migrations |

---

## License & Author

MIT © [T4VN](https://github.com/T4VN). Issues and contributions welcome on the
[GitHub repository](https://github.com/T4VN/uTPro.Feature.SimpleFormBuilder).
