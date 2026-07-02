using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

class DIuTProSimpleFormService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        // Singleton (not Scoped): the service opens its own DB scope per method via
        // IScopeProvider and has only singleton dependencies. Being a singleton lets
        // it be injected into the Form Picker value editor, which Umbraco builds from
        // the root service provider (scoped services can't be resolved there).
        => builder.Services.AddSingleton<IuTProSimpleFormService, uTProSimpleFormService>();
}

public interface IuTProSimpleFormService
{
    List<FormViewModel> GetAllForms();
    FormViewModel? GetForm(int id);
    FormViewModel? GetFormByAlias(string alias);
    (bool Success, string Message, int Id) SaveForm(SaveFormRequest request);
    (bool Success, string Message) DeleteForm(int id);
    FormExportModel? ExportForm(int id);
    (bool Success, string Message, int Id) ImportForm(FormExportModel model);
    (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua);
    PagedResult<EntryViewModel> GetEntries(int formId, int skip, int take, bool canViewSensitive = false, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    (bool Success, string Message) DeleteEntry(int id);
}

internal class uTProSimpleFormService(
    IScopeProvider scopeProvider,
    ILogger<uTProSimpleFormService> logger,
    IDataProtectionProvider dataProtectionProvider) : IuTProSimpleFormService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string ProtectorPurpose = "uTPro.uTProSimpleForm.SensitiveField";
    private const string EncryptedPrefix = "uTProEncode:";
    private const string MaskedValue = "*****";

    private IDataProtector Protector => dataProtectionProvider.CreateProtector(ProtectorPurpose);

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

    public (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var form = scope.Database.SingleOrDefault<uTProSimpleFormDto>(
                scope.SqlContext.Sql().SelectAll().From<uTProSimpleFormDto>()
                    .Where<uTProSimpleFormDto>(x => x.Alias == alias));
            if (form == null) return (false, "Form not found");
            if (!form.IsEnabled) return (false, "Form is disabled");

            var fields = string.IsNullOrEmpty(form.FieldsJson)
                ? [] : JsonSerializer.Deserialize<List<FormFieldViewModel>>(form.FieldsJson, JsonOpts) ?? [];

            // Collect fields from groups → columns → fields
            var groups = string.IsNullOrEmpty(form.GroupsJson)
                ? [] : JsonSerializer.Deserialize<List<FormGroupViewModel>>(form.GroupsJson, JsonOpts) ?? [];
            var allFields = groups.SelectMany(g => g.Columns.SelectMany(c => c.Fields)).ToList();
            // Include any legacy ungrouped fields for backward compatibility
            allFields.AddRange(fields);

            foreach (var f in allFields.Where(f => f.Required && !f.IsHidden))
            {
                if (!data.TryGetValue(f.Name, out var val) || string.IsNullOrWhiteSpace(val))
                    return (false, $"Field '{f.Label}' is required");
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

            return (true, form.SuccessMessage ?? "Thank you for your submission!");
        }
        catch (Exception ex)
        {
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
            scope.Database.DeleteMany<uTProSimpleFormEntryDto>().Where(x => x.Id == id).Execute();
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
}
