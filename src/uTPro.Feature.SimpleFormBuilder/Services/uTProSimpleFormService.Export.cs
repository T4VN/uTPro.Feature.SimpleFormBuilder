using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Extensions;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

// Bulk ZIP export: one folder per entry (its CSV row + uploaded files) with CSV hardening.
internal partial class uTProSimpleFormService
{
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

        // Cap the number of rows materialised so a very large result set can't exhaust
        // memory (rows + parsed data + the in-memory ZIP are all bounded by this). The cap
        // is applied in SQL via paging, so oversized exports never leave the database.
        var maxExport = DefaultMaxExportEntries;
        if (int.TryParse(configuration["uTPro:Feature:Form:MaxExportEntries"], out var configuredMax) && configuredMax > 0)
            maxExport = configuredMax;

        var entries = scope.Database.SkipTake<uTProSimpleFormEntryDto>(0, maxExport, sql);

        // Parse every entry's data once, and build a stable, shared column order:
        // form field definitions first (skipping layout-only types), then any extra keys.
        var parsed = entries
            .Select(e => (Entry: e, Data: string.IsNullOrEmpty(e.DataJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(e.DataJson, JsonOpts) ?? []))
            .ToList();

        // Track membership in a HashSet so the column list stays de-duplicated in first-seen
        // order without the previous O(n²) List.Contains scans (matters for wide forms /
        // many entries with distinct keys). Ordering and final set are unchanged.
        var columns = new List<string>();
        var seenColumns = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in GetAllFormFields(form))
            if (f.Type != "div" && f.Type != "step" && !string.IsNullOrEmpty(f.Name) && seenColumns.Add(f.Name))
                columns.Add(f.Name);
        foreach (var (_, data) in parsed)
            foreach (var key in data.Keys)
                if (seenColumns.Add(key)) columns.Add(key);

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
    {
        var v = value ?? string.Empty;
        // CSV/formula-injection guard: neutralise values a spreadsheet could execute as a
        // formula by prefixing a single quote before the normal quoting/escaping.
        var trimmed = v.TrimStart();
        if (trimmed.Length > 0 && "=+-@\t\r".IndexOf(trimmed[0]) >= 0)
            v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

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
}
