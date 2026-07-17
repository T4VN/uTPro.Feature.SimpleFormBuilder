using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

// Core service: form CRUD + import/export mapping, shared configuration/fields, and the
// helpers reused across the entries/files/export/validation partials.
internal partial class uTProSimpleFormService(
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

    // Server-side submission hardening.
    // Max length allowed for a normal (non-file) submitted value.
    private const int MaxFieldLength = 8000;
    // Default cap applied when a file field has no explicit "maxSize" (MB).
    private const double DefaultMaxUploadMb = 10;
    // Upper bound on the number of entries a single ZIP export will materialise, keeping
    // memory bounded (all matching rows are otherwise loaded + zipped in memory). Override
    // via uTPro:Feature:Form:MaxExportEntries; values <= 0 fall back to this default.
    private const int DefaultMaxExportEntries = 10000;
    // When a file field has no "accept" allow-list, reject these dangerous/executable/script
    // extensions by default (defence in depth — uploads are stored outside wwwroot anyway).
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "exe", "dll", "bat", "cmd", "com", "msi", "ps1", "sh", "cshtml", "razor", "aspx",
        "asp", "php", "jsp", "svg", "html", "htm", "js", "vbs", "jar"
    };
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    // Flattens groups → columns → fields plus any legacy ungrouped fields. Shared by the
    // submission (validation) and export partials; deserialises Groups/Fields JSON once.
    private static List<FormFieldViewModel> GetAllFormFields(uTProSimpleFormDto form)
    {
        var groups = string.IsNullOrEmpty(form.GroupsJson)
            ? [] : JsonSerializer.Deserialize<List<FormGroupViewModel>>(form.GroupsJson, JsonOpts) ?? [];
        var all = groups.SelectMany(g => g.Columns.SelectMany(c => c.Fields)).ToList();
        if (!string.IsNullOrEmpty(form.FieldsJson))
            all.AddRange(JsonSerializer.Deserialize<List<FormFieldViewModel>>(form.FieldsJson, JsonOpts) ?? []);
        return all;
    }
}
