using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

// Entries: query (paged, filtered), delete, and entry → view-model mapping.
internal partial class uTProSimpleFormService
{
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
