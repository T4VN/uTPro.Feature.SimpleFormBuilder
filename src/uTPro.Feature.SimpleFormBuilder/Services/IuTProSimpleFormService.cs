using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

public interface IuTProSimpleFormService
{
    List<FormViewModel> GetAllForms();
    FormViewModel? GetForm(int id);
    FormViewModel? GetFormByAlias(string alias);
    (bool Success, string Message, int Id) SaveForm(SaveFormRequest request);
    (bool Success, string Message) DeleteForm(int id);
    FormExportModel? ExportForm(int id);
    (bool Success, string Message, int Id) ImportForm(FormExportModel model);
    (bool Success, string Message) SubmitForm(string alias, Dictionary<string, string> data, IReadOnlyList<FormFileUpload> files, string? ip, string? ua);
    PagedResult<EntryViewModel> GetEntries(int formId, int skip, int take, bool canViewSensitive = false, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null);
    (bool Success, string Message) DeleteEntry(int id);

    /// <summary>Resolves the physical file behind an entry's file-field value for download.
    /// Sensitive (encrypted) file fields require <paramref name="canViewSensitive"/> to be true.</summary>
    EntryFileResult? GetEntryFile(int entryId, string fieldName, bool canViewSensitive = false);

    /// <summary>Builds a ZIP of all matching entries: one folder per entry (named by id)
    /// containing that entry's CSV row and any uploaded files.</summary>
    EntriesExportResult? ExportEntriesZip(int formId, bool canViewSensitive, string? search, DateTime? dateFrom, DateTime? dateTo);
}
