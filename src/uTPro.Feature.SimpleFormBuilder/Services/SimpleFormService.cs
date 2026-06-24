using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Scoping;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

class DISimpleFormService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<ISimpleFormService, SimpleFormService>();
}

public interface ISimpleFormService
{
    List<FormViewModel> GetAllForms();
    FormViewModel? GetForm(int id);
    FormViewModel? GetFormByAlias(string alias);
    (bool Success, string Message, int Id) SaveForm(SaveFormRequest request);
    (bool Success, string Message) DeleteForm(int id);
    (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua);
    PagedResult<EntryViewModel> GetEntries(int formId, int skip, int take, bool canViewSensitive = false, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    (bool Success, string Message) DeleteEntry(int id);
}

internal class SimpleFormService(
    IScopeProvider scopeProvider,
    ILogger<SimpleFormService> logger,
    IDataProtectionProvider dataProtectionProvider) : ISimpleFormService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string ProtectorPurpose = "uTPro.SimpleForm.SensitiveField";
    private const string EncryptedPrefix = "🔒:";
    private const string MaskedValue = "*****";

    private IDataProtector Protector => dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public List<FormViewModel> GetAllForms()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dtos = scope.Database.Fetch<SimpleFormDto>("SELECT * FROM utpro_SimpleForm ORDER BY Name");
        return dtos.Select(MapToViewModel).ToList();
    }

    public FormViewModel? GetForm(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dto = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Id = @0", id);
        return dto == null ? null : MapToViewModel(dto);
    }

    public FormViewModel? GetFormByAlias(string alias)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var dto = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", alias);
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
                var existing = db.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Id = @0", request.Id);
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
                existing.UpdatedUtc = now;
                db.Update(existing);
                return (true, "Form updated", existing.Id);
            }
            else
            {
                var dup = db.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", request.Alias);
                if (dup != null) return (false, "Alias already exists", 0);

                var dto = new SimpleFormDto
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
            scope.Database.Execute("DELETE FROM utpro_SimpleFormEntry WHERE FormId = @0", id);
            scope.Database.Execute("DELETE FROM utpro_SimpleForm WHERE Id = @0", id);
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting form {Id}", id);
            return (false, ex.Message);
        }
    }

    public (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, string? ip, string? ua)
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            var form = scope.Database.SingleOrDefault<SimpleFormDto>("SELECT * FROM utpro_SimpleForm WHERE Alias = @0", alias);
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
                var entry = new SimpleFormEntryDto
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
        var sql = scope.SqlContext.Sql()
            .Select("*").From("utpro_SimpleFormEntry")
            .Where("FormId = @0", formId);

        if (dateFrom.HasValue)
            sql = sql.Where("CreatedUtc >= @0", dateFrom.Value.Date);
        if (dateTo.HasValue)
            sql = sql.Where("CreatedUtc < @0", dateTo.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(search))
            sql = sql.Where("(DataJson LIKE @0 OR IpAddress LIKE @0)", $"%{search}%");

        sql = sql.OrderByDescending("CreatedUtc");

        var page = db.Page<SimpleFormEntryDto>(skip / Math.Max(take, 1) + 1, take, sql);
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
            scope.Database.Execute("DELETE FROM utpro_SimpleFormEntry WHERE Id = @0", id);
            return (true, "Deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting entry {Id}", id);
            return (false, ex.Message);
        }
    }

    private static FormViewModel MapToViewModel(SimpleFormDto dto) => new()
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
        CreatedUtc = dto.CreatedUtc, UpdatedUtc = dto.UpdatedUtc
    };

    private EntryViewModel MapEntry(SimpleFormEntryDto dto, bool canViewSensitive = false)
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
