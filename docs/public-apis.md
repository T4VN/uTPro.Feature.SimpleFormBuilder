# Public APIs

[← Back to README](../README.md)

REST endpoints for headless or hybrid use cases. These are **anonymous** and bypass backoffice roles — enable them deliberately.

## Submit a form (always available)

```http
POST /api/utpro/simple-form/submit
Content-Type: application/json

{
  "alias": "contact-us",
  "data": { "name": "Jane", "email": "jane@example.com", "message": "Hello!" }
}
```

### Submitting file uploads (`v2.1.0+`)

Forms that contain a `file` field submit as **`multipart/form-data`** so the files travel
with the submission. The built-in front-end script does this automatically; to call it
yourself, send:

- `alias` — the form alias
- `data` — a JSON string of the non-file field values
- one part per file, named `file:{fieldName}` (e.g. `file:cv`)

```http
POST /api/utpro/simple-form/submit
Content-Type: multipart/form-data; boundary=...

alias=contact-us
data={"name":"Jane","email":"jane@example.com"}
file:cv=<binary>
```

Files are validated server-side against the field's `accept` / `maxSize` settings and are
only written to disk once the whole submission validates and is stored — a failed or
abandoned submit never leaves orphaned files. The JSON form above still works for
file-less submissions (backward compatible).

Stored files live **outside `wwwroot`** (under `App_Data/umbraco/Data/uTProSimpleFormUploads`)
and are **not** publicly accessible. In an entry they are recorded as a
`utpro-file:{fileName}|{token}` reference, where `{token}` is an encrypted path — the
client never sees the real location. Downloading is only possible through the
authenticated backoffice endpoint below.

## Get form definition (opt-in per form: *Enable Render API*)

```http
GET /api/utpro/simple-form/render/{alias}
```

## Get form entries (opt-in per form: *Enable Entries API*, sensitive data always masked)

```http
GET /api/utpro/simple-form/entries/{alias}?skip=0&take=20
```

> ⚠️ Enabling the Entries API exposes that form's submissions (sensitive fields masked) to anyone who knows the alias. Use with care.

## Backoffice-only: file download & ZIP export (`v2.1.0+`)

These endpoints live on the **backoffice management API** and require a valid backoffice
login (they are **not** anonymous). Sensitive file fields require the *Sensitive Data*
permission — otherwise the file is treated as masked and denied.

**Download a single uploaded file** (streams the stored file):

```http
GET /umbraco/management/api/v1/utpro/simple-form/entry-file?entryId={id}&fieldName={name}
```

**Export all entries as a ZIP** — one folder per entry (named by its id), each containing
that entry's CSV row and any uploaded files. Accepts the same filters as the entries list:

```http
POST /umbraco/management/api/v1/utpro/simple-form/export-entries-zip
Content-Type: application/json

{ "formId": 1, "search": null, "dateFrom": null, "dateTo": null }
```

In the backoffice these are surfaced by the file download button on each entry and by the
**Export → Export data + files (ZIP)** action in the entries toolbar.

## Import / Export

![Import / Export](../Image/Screenshots/import-export.png)

Forms can be moved between environments as JSON (definition only — **no entries, no IDs, no timestamps**).

- **Export** — from the editor toolbar, a form row, or the sidebar **⋯** menu. Downloads `{alias}.form.json`. If the open form has unsaved changes you're asked to save first.
- **Import** — from the list toolbar or sidebar **Options** menu. Always creates a **new** form; if the alias already exists it is auto-suffixed (`-copy`, `-copy-2`, …).

Requires edit permission (`canEdit`).
