using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

internal class uTProSimpleFormService(
    IScopeProvider scopeProvider,
    ILogger<uTProSimpleFormService> logger,
    IDataProtectionProvider dataProtectionProvider,
    IWebHostEnvironment webHostEnvironment,
    IConfiguration configuration) : IuTProSimpleFormService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string ProtectorPurpose = "uTPro.uTProSimpleForm.SensitiveField";
    private const string EncryptedPrefix = "uTProEncode:";
    private const string MaskedValue = "*****";

    // File-upload storage. Files live OUTSIDE wwwroot (not publicly served) and are
    // streamed only through the authenticated backoffice download endpoint.
    private const string FilePrefix = "utpro-file:";
    private const string FileTokenPurpose = "uTPro.uTProSimpleForm.FileToken";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private IDataProtector Protector => dataProtectionProvider.CreateProtector(ProtectorPurpose);
    private IDataProtector FileTokenProtector => dataProtectionProvider.CreateProtector(FileTokenPurpose);
    // Stored OUTSIDE wwwroot (never served statically — only streamed through the
    // authenticated backoffice endpoint). Location can be overridden via
    // uTPro:Feature:Form:FileUploadsPath so load-balanced apps (backoffice + website)
    // can share one physical folder; a relative value is resolved against the content
    // root. Empty (default) = <ContentRoot>\umbraco\Data\uTProSimpleFormUploads.
    private string FileUploadsRoot
    {
        get
        {
            var configured = configuration["uTPro:Feature:Form:FileUploadsPath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.IsPathRooted(configured)
                    ? configured
                    : Path.GetFullPath(Path.Combine(webHostEnvironment.ContentRootPath, configured));
            }

            return Path.Combine(
                webHostEnvironment.ContentRootPath, "umbraco", "Data", "uTProSimpleFormUploads");
        }
    }

    public List<FormViewModel> GetAllForms()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);

        var sql = scope.SqlContext.Sql()
            .SelectAll().From<uTProSimpleFormDto>()
            .OrderBy<uTProSimpleFormDto>(x => x.Name);
        var dtos = scope.Database.Fetch<uTProSimpleFormDto>(sql);
        var models = dtos.Select(MapToViewModel).ToList();

        // Populate entry counts in a single grouped query. Identifiers are quoted via the
        // active SQL syntax provider so the aggregate works across SQL Server, SQLite and
        // PostgreSQL (which folds unquoted identifiers to lower-case).
        var syntax = scope.SqlContext.SqlSyntax;
        var entryTable = syntax.GetQuotedTableName("uTProSimpleFormEntry");
        var formIdCol = syntax.GetQuotedColumnName("FormId");
        var counts = scope.Database
            .Fetch<EntryCountRow>($"SELECT {formIdCol} AS FormId, COUNT(*) AS Cnt FROM {entryTable} GROUP BY {formIdCol}")
            .ToDictionary(r => r.FormId, r => (int)r.Cnt);
        foreach (var m in models)
            m.EntryCount = counts.TryGetValue(m.Id, out var c) ? c : 0;

        return models;
    }

    private class EntryCountRow
    {
        public int FormId { get; set; }
        public long Cnt { get; set; }
    }

    public FormViewModel? GetForm(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
            .Where<uTProSimpleFormDto>(x => x.Id == id);
        var dto = scope.Database.SingleOrDefault<uTProSimpleFormDto>(sql);
        return dto == null ? null : MapToViewModel(dto);
    }

    public FormViewModel? GetFormByAlias(string alias)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
            .Where<uTProSimpleFormDto>(x => x.Alias == alias);
        var dto = scope.Database.SingleOrDefault<uTProSimpleFormDto>(sql);
        return dto == null ? null : MapToViewModel(dto);
    }

    public (bool Success, string Message, int Id) SaveForm(SaveFormRequest request)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var db = scope.Database;
            var now = DateTime.UtcNow;
            var fieldsJson = JsonSerializer.Serialize(request.Fields, JsonOpts);
            var groupsJson = JsonSerializer.Serialize(request.Groups, JsonOpts);

            if (request.Id > 0)
            {
                var existing = db.SingleOrDefault<uTProSimpleFormDto>(
                    scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                        .Where<uTProSimpleFormDto>(x => x.Id == request.Id));
                if (existing == null) return (false, "Form not found", 0);

                existing.Name = request.Name;
                existing.Alias = request.Alias;
                existing.FieldsJson = fieldsJson;
                existing.GroupsJson = groupsJson;
                existing.SuccessMessage = request.SuccessMessage;
                existing.RedirectUrl = request.RedirectUrl;
                existing.EmailTo = request.EmailTo;
                existing.EmailSubject = request.EmailSubject;
                existing.StoreEntries = request.StoreEntries;
                existing.IsEnabled = request.IsEnabled;
                existing.VisibleColumnsJson = request.VisibleColumns != null
                    ? JsonSerializer.Serialize(request.VisibleColumns, JsonOpts) : null;
                existing.EnableRenderApi = request.EnableRenderApi;
                existing.EnableEntriesApi = request.EnableEntriesApi;
                existing.ShowInPicker = request.ShowInPicker;
                existing.UpdatedUtc = now;
                db.Update(existing);
                return (true, "Form updated", existing.Id);
            }
            else
            {
                var dup = db.SingleOrDefault<uTProSimpleFormDto>(
                    scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                        .Where<uTProSimpleFormDto>(x => x.Alias == request.Alias));
                if (dup != null) return (false, "Alias already exists", 0);

                var dto = new uTProSimpleFormDto
                {
                    Name = request.Name,
                    Alias = request.Alias,
                    FieldsJson = fieldsJson,
                    GroupsJson = groupsJson,
                    SuccessMessage = request.SuccessMessage,
                    RedirectUrl = request.RedirectUrl,
                    EmailTo = request.EmailTo,
                    EmailSubject = request.EmailSubject,
                    StoreEntries = request.StoreEntries,
                    IsEnabled = request.IsEnabled,
                    VisibleColumnsJson = request.VisibleColumns != null
                        ? JsonSerializer.Serialize(request.VisibleColumns, JsonOpts) : null,
                    EnableRenderApi = request.EnableRenderApi,
                    EnableEntriesApi = request.EnableEntriesApi,
                    ShowInPicker = request.ShowInPicker,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                db.Insert(dto);
                return (true, "Form created", dto.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving form");
            return (false, ex.Message, 0);
        }
    }

    public (bool Success, string Message) DeleteForm(int id)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            // Clean up uploaded files for every entry before removing the rows.
            var entries = scope.Database.Fetch<uTProSimpleFormEntryDto>(
                scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormEntryDto>()
                    .Where<uTProSimpleFormEntryDto>(x => x.FormId == id));
            foreach (var entry in entries)
                DeleteStoredFiles(ExtractFileValues(entry.DataJson));

            scope.Database.DeleteMany<uTProSimpleFormEntryDto>().Where(x => x.FormId == id).Execute();
            scope.Database.DeleteMany<uTProSimpleFormDto>().Where(x => x.Id == id).Execute();
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting form {Id}", id);
            return (false, ex.Message);
        }
    }

    public FormExportModel? ExportForm(int id)
    {
        var form = GetForm(id);
        if (form == null) return null;
        return new FormExportModel
        {
            Name = form.Name,
            Alias = form.Alias,
            Fields = form.Fields,
            Groups = form.Groups,
            SuccessMessage = form.SuccessMessage,
            RedirectUrl = form.RedirectUrl,
            EmailTo = form.EmailTo,
            EmailSubject = form.EmailSubject,
            StoreEntries = form.StoreEntries,
            IsEnabled = form.IsEnabled,
            VisibleColumns = form.VisibleColumns,
            EnableRenderApi = form.EnableRenderApi,
            EnableEntriesApi = form.EnableEntriesApi,
            ShowInPicker = form.ShowInPicker
        };
    }

    public (bool Success, string Message, int Id) ImportForm(FormExportModel model)
    {
        if (model == null) return (false, "Invalid import file", 0);
        if (string.IsNullOrWhiteSpace(model.Name) && string.IsNullOrWhiteSpace(model.Alias))
            return (false, "Import file does not contain a valid form", 0);

        var name = string.IsNullOrWhiteSpace(model.Name) ? model.Alias : model.Name;
        var baseAlias = string.IsNullOrWhiteSpace(model.Alias)
            ? Slugify(name)
            : model.Alias;

        var request = new SaveFormRequest
        {
            Id = 0,
            Name = name,
            Alias = EnsureUniqueAlias(baseAlias),
            Fields = model.Fields ?? [],
            Groups = model.Groups ?? [],
            SuccessMessage = model.SuccessMessage,
            RedirectUrl = model.RedirectUrl,
            EmailTo = model.EmailTo,
            EmailSubject = model.EmailSubject,
            StoreEntries = model.StoreEntries,
            IsEnabled = model.IsEnabled,
            VisibleColumns = model.VisibleColumns,
            EnableRenderApi = model.EnableRenderApi,
            EnableEntriesApi = model.EnableEntriesApi,
            ShowInPicker = model.ShowInPicker
        };

        return SaveForm(request);
    }

    // Returns the alias unchanged when free, otherwise appends -copy, -copy-2, ... until unique.
    private string EnsureUniqueAlias(string alias)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        bool Exists(string a) => db.SingleOrDefault<uTProSimpleFormDto>(
            scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                .Where<uTProSimpleFormDto>(x => x.Alias == a)) != null;

        if (!Exists(alias)) return alias;

        var candidate = alias + "-copy";
        var i = 2;
        while (Exists(candidate))
            candidate = $"{alias}-copy-{i++}";
        return candidate;
    }

    private static string Slugify(string input)
    {
        var slug = new string((input ?? string.Empty).ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "form" : slug;
    }

    public (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, IReadOnlyList<FormFileUpload> files, string? ip, string? ua)
    {
        // Files are written to disk only after validation passes; track them so any
        // later failure (or a non-storing form) rolls the writes back — no orphans.
        var savedPaths = new List<string>();
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var form = scope.Database.SingleOrDefault<uTProSimpleFormDto>(
                scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                    .Where<uTProSimpleFormDto>(x => x.Alias == alias));
            if (form == null) return (false, "Form not found");
            if (!form.IsEnabled) return (false, "Form is disabled");

            var allFields = GetAllFormFields(form);

            // Persist uploaded files (validate type/size), mapping each to its file field.
            var fileFields = new Dictionary<string, FormFieldViewModel>(StringComparer.Ordinal);
            foreach (var f in allFields.Where(f => f.Type == "file"))
                fileFields.TryAdd(f.Name, f);

            foreach (var upload in files ?? [])
            {
                if (upload.File == null || upload.File.Length == 0) continue;
                // Ignore files that don't map to a known file field (defensive).
                if (!fileFields.TryGetValue(upload.FieldName, out var field)) continue;

                var (ok, message, value, fullPath) = SaveUploadedFileInternal(form, field, upload.File);
                if (!ok)
                {
                    RollbackFiles(savedPaths);
                    return (false, message);
                }
                savedPaths.Add(fullPath);
                data[upload.FieldName] = value;
            }

            foreach (var f in allFields.Where(f => f.Required && !f.IsHidden))
            {
                if (!data.TryGetValue(f.Name, out var val) || string.IsNullOrWhiteSpace(val))
                {
                    RollbackFiles(savedPaths);
                    return (false, $"Field '{f.Label}' is required");
                }
            }

            // Encrypt sensitive fields
            var sensitiveNames = allFields
                .Where(f => f.IsSensitive || f.Type == "password")
                .Select(f => f.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var storageData = new Dictionary<string, string>(data);
            foreach (var key in storageData.Keys.Where(k => sensitiveNames.Contains(k)).ToList())
            {
                var raw = storageData[key];
                if (!string.IsNullOrEmpty(raw))
                {
                    storageData[key] = EncryptedPrefix + Protector.Protect(raw);
                }
            }

            if (form.StoreEntries)
            {
                var entry = new uTProSimpleFormEntryDto
                {
                    FormId = form.Id,
                    DataJson = JsonSerializer.Serialize(storageData, JsonOpts),
                    IpAddress = ip,
                    UserAgent = ua?.Length > 500 ? ua[..500] : ua,
                    CreatedUtc = DateTime.UtcNow
                };
                scope.Database.Insert(entry);
            }
            else
            {
                // Nothing is persisted for this form, so drop any files we just wrote.
                RollbackFiles(savedPaths);
            }

            return (true, form.SuccessMessage ?? "Thank you for your submission!");
        }
        catch (Exception ex)
        {
            RollbackFiles(savedPaths);
            logger.LogError(ex, "Error submitting form {Alias}", alias);
            return (false, ex.Message);
        }
    }

    public PagedResult<EntryViewModel> GetEntries(int formId, int skip, int take, bool canViewSensitive = false, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        var syntax = scope.SqlContext.SqlSyntax;

        var sql = scope.SqlContext.Sql()
            .SelectAll().From<uTProSimpleFormEntryDto>()
            .Where<uTProSimpleFormEntryDto>(x => x.FormId == formId);

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            sql = sql.Where<uTProSimpleFormEntryDto>(x => x.CreatedUtc >= from);
        }
        if (dateTo.HasValue)
        {
            var toExclusive = dateTo.Value.Date.AddDays(1);
            sql = sql.Where<uTProSimpleFormEntryDto>(x => x.CreatedUtc < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Quote identifiers per provider and use a plain LIKE. We intentionally do NOT
            // wrap the column in LOWER(): on databases upgraded from v1 the JSON column is
            // still NTEXT, and SQL Server rejects LOWER(ntext). Plain LIKE works on ntext,
            // text and nvarchar alike. SQL Server / SQLite stay case-insensitive via their
            // default collation; PostgreSQL LIKE is case-sensitive.
            var dataCol = syntax.GetQuotedColumnName("DataJson");
            var ipCol = syntax.GetQuotedColumnName("IpAddress");
            sql = sql.Where($"({dataCol} LIKE @0 OR {ipCol} LIKE @0)", $"%{search}%");
        }

        sql = sql.OrderByDescending<uTProSimpleFormEntryDto>(x => x.CreatedUtc);

        var page = db.Page<uTProSimpleFormEntryDto>(skip / Math.Max(take, 1) + 1, take, sql);
        return new PagedResult<EntryViewModel>
        {
            Items = page.Items.Select(s => MapEntry(s, canViewSensitive)),
            Total = page.TotalItems
        };
    }

    public (bool Success, string Message) DeleteEntry(int id)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var entry = scope.Database.SingleOrDefault<uTProSimpleFormEntryDto>(
                scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormEntryDto>()
                    .Where<uTProSimpleFormEntryDto>(x => x.Id == id));
            if (entry != null)
            {
                DeleteStoredFiles(ExtractFileValues(entry.DataJson));
                scope.Database.DeleteMany<uTProSimpleFormEntryDto>().Where(x => x.Id == id).Execute();
            }
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting entry {Id}", id);
            return (false, ex.Message);
        }
    }

    private static FormViewModel MapToViewModel(uTProSimpleFormDto dto) => new()
    {
        Id = dto.Id, Name = dto.Name, Alias = dto.Alias,
        Fields = string.IsNullOrEmpty(dto.FieldsJson)
            ? [] : JsonSerializer.Deserialize<List<FormFieldViewModel>>(dto.FieldsJson, JsonOpts) ?? [],
        Groups = string.IsNullOrEmpty(dto.GroupsJson)
            ? [] : JsonSerializer.Deserialize<List<FormGroupViewModel>>(dto.GroupsJson, JsonOpts) ?? [],
        SuccessMessage = dto.SuccessMessage, RedirectUrl = dto.RedirectUrl,
        EmailTo = dto.EmailTo, EmailSubject = dto.EmailSubject,
        StoreEntries = dto.StoreEntries, IsEnabled = dto.IsEnabled,
        VisibleColumns = string.IsNullOrEmpty(dto.VisibleColumnsJson)
            ? null : JsonSerializer.Deserialize<List<string>>(dto.VisibleColumnsJson, JsonOpts),
        EnableRenderApi = dto.EnableRenderApi,
        EnableEntriesApi = dto.EnableEntriesApi,
        ShowInPicker = dto.ShowInPicker,
        CreatedUtc = dto.CreatedUtc, UpdatedUtc = dto.UpdatedUtc
    };

    private EntryViewModel MapEntry(uTProSimpleFormEntryDto dto, bool canViewSensitive = false)
    {
        var data = string.IsNullOrEmpty(dto.DataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(dto.DataJson, JsonOpts) ?? [];

        foreach (var key in data.Keys.ToList())
        {
            var val = data[key];
            if (val != null && val.StartsWith(EncryptedPrefix))
            {
                if (canViewSensitive)
                {
                    try { data[key] = Protector.Unprotect(val[EncryptedPrefix.Length..]); }
                    catch { data[key] = "[decryption error]"; }
                }
                else { data[key] = MaskedValue; }
            }
        }

        return new EntryViewModel
        {
            Id = dto.Id, FormId = dto.FormId,
            Data = data,
            IpAddress = dto.IpAddress, CreatedUtc = dto.CreatedUtc
        };
    }

    // ── File uploads ──

    // Validates and writes a single uploaded file for a known file field.
    // Returns the "utpro-file:" value to persist plus the physical path (for rollback).
    private (bool Success, string Message, string Value, string FullPath) SaveUploadedFileInternal(
        uTProSimpleFormDto form, FormFieldViewModel field, IFormFile file)
    {
        // Validate size against the field's "maxSize" (MB) attribute.
        var maxSizeMb = ParseDouble(field.Attributes?.GetValueOrDefault("maxSize"));
        if (maxSizeMb is > 0 && file.Length > maxSizeMb.Value * 1024 * 1024)
            return (false, $"File for '{field.Label}' exceeds the maximum size of {maxSizeMb.Value:0.##} MB", string.Empty, string.Empty);

        // Validate extension against the field's "accept" attribute (e.g. ".pdf,.jpg").
        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName);
        var accept = field.Attributes?.GetValueOrDefault("accept");
        if (!IsExtensionAllowed(ext, accept))
            return (false, $"File type '{ext}' is not allowed for '{field.Label}'", string.Empty, string.Empty);

        // Persist to a non-public folder: {root}/{alias}/{yyyyMM}/{guid}{ext}
        var safeAlias = SanitizeSegment(form.Alias);
        var subFolder = DateTime.UtcNow.ToString("yyyyMM");
        var targetDir = Path.Combine(FileUploadsRoot, safeAlias, subFolder);
        Directory.CreateDirectory(targetDir);

        var storedName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(targetDir, storedName);
        using (var target = File.Create(fullPath))
        {
            file.CopyTo(target);
        }

        // Relative path is what we protect; the client never sees it.
        var relativePath = string.Join('/', safeAlias, subFolder, storedName);
        var token = FileTokenProtector.Protect(relativePath);
        // Keep the display name free of the delimiter so parsing stays trivial.
        var displayName = originalName.Replace('|', '_');
        var value = $"{FilePrefix}{displayName}|{token}";
        return (true, "Uploaded", value, fullPath);
    }

    // Deletes physical files by absolute path (used to roll back a failed submission).
    private void RollbackFiles(IEnumerable<string> fullPaths)
    {
        foreach (var path in fullPaths)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to roll back uploaded file {Path}", path); }
        }
    }

    // ── Bulk ZIP export ──

    public EntriesExportResult? ExportEntriesZip(int formId, bool canViewSensitive, string? search, DateTime? dateFrom, DateTime? dateTo)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var form = scope.Database.SingleOrDefault<uTProSimpleFormDto>(
            scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                .Where<uTProSimpleFormDto>(x => x.Id == formId));
        if (form == null) return null;

        // Same filtering as GetEntries, but no paging — export everything that matches.
        var syntax = scope.SqlContext.SqlSyntax;
        var sql = scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormEntryDto>()
            .Where<uTProSimpleFormEntryDto>(x => x.FormId == formId);
        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.Date;
            sql = sql.Where<uTProSimpleFormEntryDto>(x => x.CreatedUtc >= from);
        }
        if (dateTo.HasValue)
        {
            var toExclusive = dateTo.Value.Date.AddDays(1);
            sql = sql.Where<uTProSimpleFormEntryDto>(x => x.CreatedUtc < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var dataCol = syntax.GetQuotedColumnName("DataJson");
            var ipCol = syntax.GetQuotedColumnName("IpAddress");
            sql = sql.Where($"({dataCol} LIKE @0 OR {ipCol} LIKE @0)", $"%{search}%");
        }
        sql = sql.OrderBy<uTProSimpleFormEntryDto>(x => x.CreatedUtc);

        var entries = scope.Database.Fetch<uTProSimpleFormEntryDto>(sql);

        // Parse every entry's data once, and build a stable, shared column order:
        // form field definitions first (skipping layout-only types), then any extra keys.
        var parsed = entries
            .Select(e => (Entry: e, Data: string.IsNullOrEmpty(e.DataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(e.DataJson, JsonOpts) ?? []))
            .ToList();

        var columns = new List<string>();
        foreach (var f in GetAllFormFields(form))
            if (f.Type != "div" && f.Type != "step" && !string.IsNullOrEmpty(f.Name) && !columns.Contains(f.Name))
                columns.Add(f.Name);
        foreach (var (_, data) in parsed)
            foreach (var key in data.Keys)
                if (!columns.Contains(key)) columns.Add(key);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entry, data) in parsed)
            {
                var folder = entry.Id.ToString();

                // Resolve each column into a display cell and (for file fields) a physical path.
                var cells = new List<string>();
                var filesToAdd = new List<(string Name, string Path)>();
                foreach (var col in columns)
                {
                    data.TryGetValue(col, out var raw);
                    var (cell, filePath, fileName) = ResolveExportCell(raw, canViewSensitive);
                    cells.Add(cell);
                    if (filePath != null) filesToAdd.Add((fileName, filePath));
                }

                // Per-entry CSV: header + this single row (mirrors the CSV export layout).
                var header = new List<string> { "Date", "IP" };
                header.AddRange(columns);
                var row = new List<string>
                {
                    entry.CreatedUtc.ToString("u"),
                    entry.IpAddress ?? string.Empty
                };
                row.AddRange(cells);

                var csv = new StringBuilder();
                csv.Append('\uFEFF'); // BOM so Excel reads UTF-8 correctly
                csv.AppendLine(string.Join(",", header.Select(CsvCell)));
                csv.AppendLine(string.Join(",", row.Select(CsvCell)));

                WriteZipText(zip, $"{folder}/entry-{entry.Id}.csv", csv.ToString());

                // Uploaded files for this entry, de-duplicated within the folder.
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (name, path) in filesToAdd)
                {
                    var safeName = UniqueName(usedNames, string.IsNullOrEmpty(name) ? "file" : name);
                    try
                    {
                        var zipEntry = zip.CreateEntry($"{folder}/{safeName}", CompressionLevel.Optimal);
                        using var zs = zipEntry.Open();
                        using var fs = File.OpenRead(path);
                        fs.CopyTo(zs);
                    }
                    catch (Exception ex) { logger.LogWarning(ex, "Failed to add file {Path} to export", path); }
                }
            }
        }

        var zipFileName = $"{SanitizeSegment(form.Alias)}-entries.zip";
        return new EntriesExportResult(ms.ToArray(), zipFileName);
    }

    // Turns a stored value into a CSV cell plus (for file fields) its physical path.
    // Sensitive values are masked when the caller lacks the Sensitive Data permission.
    private (string Cell, string? FilePath, string FileName) ResolveExportCell(string? rawValue, bool canViewSensitive)
    {
        if (string.IsNullOrEmpty(rawValue)) return (string.Empty, null, string.Empty);

        var v = rawValue;
        if (v.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            if (!canViewSensitive) return (MaskedValue, null, string.Empty);
            try { v = Protector.Unprotect(v[EncryptedPrefix.Length..]); }
            catch { return ("[decryption error]", null, string.Empty); }
        }

        if (v.StartsWith(FilePrefix, StringComparison.Ordinal))
        {
            if (TryResolveStoredFilePath(v, out var full, out var name))
                return (name, File.Exists(full) ? full : null, name);

            // Token unreadable: still surface the display name in the CSV.
            var body = v[FilePrefix.Length..];
            var sep = body.LastIndexOf('|');
            var disp = sep < 0 ? body : body[..sep];
            return (disp, null, disp);
        }

        return (v, null, string.Empty);
    }

    private static void WriteZipText(ZipArchive zip, string entryPath, string content)
    {
        var zipEntry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var stream = zipEntry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string CsvCell(string? value)
        => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    // Ensures a file name is unique within a folder by appending " (n)" before the extension.
    private static string UniqueName(HashSet<string> used, string name)
    {
        if (used.Add(name)) return name;
        var stem = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        var i = 2;
        string candidate;
        do { candidate = $"{stem} ({i++}){ext}"; } while (!used.Add(candidate));
        return candidate;
    }

    public EntryFileResult? GetEntryFile(int entryId, string fieldName, bool canViewSensitive = false)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var entry = scope.Database.SingleOrDefault<uTProSimpleFormEntryDto>(
            scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormEntryDto>()
                .Where<uTProSimpleFormEntryDto>(x => x.Id == entryId));
        if (entry == null || string.IsNullOrEmpty(entry.DataJson)) return null;

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(entry.DataJson, JsonOpts) ?? [];
        if (!data.TryGetValue(fieldName, out var value) || string.IsNullOrEmpty(value)) return null;

        // A file field marked "sensitive" is encrypted first. Mirror the entry-list masking:
        // deny the download to users without the Sensitive Data permission, then peel the layer.
        if (value.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            if (!canViewSensitive) return null;
            try { value = Protector.Unprotect(value[EncryptedPrefix.Length..]); }
            catch { return null; }
        }

        if (!TryResolveStoredFilePath(value, out var fullPath, out var displayName)) return null;
        if (!File.Exists(fullPath)) return null;

        if (!ContentTypeProvider.TryGetContentType(displayName, out var contentType))
            contentType = "application/octet-stream";

        var stream = File.OpenRead(fullPath);
        return new EntryFileResult(stream, displayName, contentType);
    }

    // Resolves a stored file-field value into a confined physical path.
    // Handles the optional "sensitive" encryption layer and the protected path token.
    private bool TryResolveStoredFilePath(string? value, out string fullPath, out string displayName)
    {
        fullPath = string.Empty;
        displayName = string.Empty;
        if (string.IsNullOrEmpty(value)) return false;

        var v = value;
        if (v.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
        {
            try { v = Protector.Unprotect(v[EncryptedPrefix.Length..]); }
            catch { return false; }
        }

        if (!v.StartsWith(FilePrefix, StringComparison.Ordinal)) return false;

        var body = v[FilePrefix.Length..];
        var sep = body.LastIndexOf('|');
        if (sep < 0) return false;

        displayName = body[..sep];
        var token = body[(sep + 1)..];

        string relativePath;
        try { relativePath = FileTokenProtector.Unprotect(token); }
        catch { return false; }

        // Confine inside the uploads root (defence in depth against traversal,
        // even though the path is protected/signed).
        var root = Path.GetFullPath(FileUploadsRoot);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !full.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            return false;

        fullPath = full;
        return true;
    }

    // Deletes the physical files behind any file-field values in the given set.
    // Safe to call with mixed data — non-file values are ignored.
    private void DeleteStoredFiles(IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            if (!TryResolveStoredFilePath(value, out var fullPath, out _)) continue;
            try { if (File.Exists(fullPath)) File.Delete(fullPath); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete uploaded file {Path}", fullPath); }
        }
    }

    // Reads the raw stored data for entries so their files can be cleaned up on delete.
    private static IEnumerable<string?> ExtractFileValues(string? dataJson)
    {
        if (string.IsNullOrEmpty(dataJson)) return [];
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson, JsonOpts);
        return data == null ? [] : data.Values.Select(v => (string?)v);
    }

    // Flattens groups → columns → fields plus any legacy ungrouped fields.
    private static List<FormFieldViewModel> GetAllFormFields(uTProSimpleFormDto form)
    {
        var groups = string.IsNullOrEmpty(form.GroupsJson)
            ? [] : JsonSerializer.Deserialize<List<FormGroupViewModel>>(form.GroupsJson, JsonOpts) ?? [];
        var all = groups.SelectMany(g => g.Columns.SelectMany(c => c.Fields)).ToList();
        if (!string.IsNullOrEmpty(form.FieldsJson))
            all.AddRange(JsonSerializer.Deserialize<List<FormFieldViewModel>>(form.FieldsJson, JsonOpts) ?? []);
        return all;
    }

    private static bool IsExtensionAllowed(string extension, string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept)) return true; // no restriction configured
        var ext = extension.TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) return false;
        return accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.TrimStart('.').ToLowerInvariant())
            .Where(a => a.Length > 0 && !a.Contains('/')) // ignore MIME patterns like "image/*"
            .Any(a => a == ext);
    }

    private static double? ParseDouble(string? s)
        => double.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;

    // Keep alias usable as a single, safe folder name.
    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((value ?? string.Empty)
            .Select(c => invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray());
        cleaned = cleaned.Trim('.', ' ');
        return string.IsNullOrEmpty(cleaned) ? "form" : cleaned;
    }
}
