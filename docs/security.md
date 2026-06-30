# Security & Permissions

[← Back to README](../README.md)

## Overview

- **Sensitive fields** (password type or fields marked sensitive) are encrypted via ASP.NET Data Protection before storage and masked in the UI by default.
- **Form management** (create/edit/delete, delete entries) requires `canEdit` (admin or Settings access).
- **Sensitive data viewing** requires admin or the `sensitiveData` group.
- **Public APIs** are disabled by default per form and must be explicitly enabled.

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

## How sensitive-data encryption works (encode / decode)

Encryption uses **ASP.NET Core Data Protection** (`IDataProtector`) — the same primitive Umbraco itself uses. Under the hood it is authenticated symmetric encryption (**AES-256-CBC + HMAC-SHA256**); the protector is created with a fixed *purpose* string (`uTPro.uTProSimpleForm.SensitiveField`).

**Encode (on submit)** — for each field whose **Type is `password`** or that has **Sensitive Data** enabled:

```
storedValue = "uTProEncode:" + Protector.Protect(rawValue)
```

The raw value is encrypted and a marker prefix (`uTProEncode:`) is prepended, then saved into the entry's `DataJson`. Non-sensitive fields are stored as-is.

**Decode (on read)** — when entries are loaded for the backoffice or the entries API, each value is checked for the `uTProEncode:` prefix:

- **Viewer may see sensitive data** (admin or `sensitiveData` group) → `Protector.Unprotect(...)` returns the original value.
- **Otherwise** → the value is replaced with `*****` (never decrypted, never sent to the client).
- If decryption fails (e.g. the key is gone) the value shows as `[decryption error]` rather than leaking ciphertext.

**Important operational notes:**

- **Encryption only applies to NEW submissions** made while the field is sensitive. Turning *Sensitive Data* on later does **not** retro-encrypt or mask values that were already stored as plain text (those have no `uTProEncode:` prefix).
- The encryption **keys are managed by ASP.NET Data Protection**, not by this package. They are persisted by the host (by default under `App_Data`/the configured key ring). **Back them up and keep them stable** — if the key ring is lost or changes, previously encrypted values can no longer be decrypted.
- On a **load-balanced / multi-server** setup, configure a **shared Data Protection key ring** (file share, Azure Blob, Redis, …) so every server can decrypt.
- The marker prefix and *purpose* string are implementation details — changing them in a future version would make existing encrypted values unreadable.

## Test Accounts (TestSite)

The bundled `TestSite` auto-seeds the accounts below on startup (see `TestUserSeeder.cs`) so the role/permission matrix can be exercised immediately — even after wiping the database. All share the unattended admin password `Admin1234!`. The seeder also creates the `sensitiveData` and `Admin Custom` user groups and grants them the *uTPro Form* section.

| Email | Group(s) | Behaviour in uTPro Form |
|---|---|---|
| `admin@example.com` | Administrators | Everything: design forms, manage entries, view sensitive data |
| `editor@example.com` | Editor *(+ uTPro Form section)* | View forms & entries, export CSV; **cannot** design/delete; sensitive shown as `*****` |
| `editorSD@example.com` | Editor + `sensitiveData` *(+ uTPro Form section)* | Same as editor, **plus** can view decrypted sensitive values |
| `adminCustom@example.com` | Admin Custom — clone of Administrators (sections incl. **Settings** + uTPro Form) | **Can design/edit/delete forms** (has Settings ⇒ `canEdit`), but sensitive values stay masked (not admin, not `sensitiveData`) |

> **Key rule:** form management (`canEdit`) is granted by the **Settings** section, not by the Administrators group alone. Sensitive-data viewing is a separate lever, granted only by the Administrators group or the `sensitiveData` group.

> The seeder is **TestSite-only** scaffolding — it is not part of the shipped package. In a real site you create users/groups through the backoffice as usual.
