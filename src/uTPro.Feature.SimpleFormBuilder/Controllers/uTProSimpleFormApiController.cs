using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Security;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.Controllers;

[VersionedApiBackOfficeRoute("utpro/simple-form")]
[ApiExplorerSettings(GroupName = "uTPro Simple Form")]
public class uTProSimpleFormApiController(
    IuTProSimpleFormService formService,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
    IEnumerable<IuTProFormFieldTypeProvider> fieldTypeProviders) : ManagementApiControllerBase
{
    // ── Permissions ──

    [HttpPost("permissions")]
    public IActionResult Permissions() => Ok(new
    {
        isAdmin = IsCurrentUserAdmin(),
        canEdit = CanCurrentUserManageForms(),
        canViewSensitive = CanCurrentUserViewSensitiveData()
    });

    // ── Forms (read: all users, write: admin only) ──

    [HttpPost("list")]
    public IActionResult List() => Ok(formService.GetAllForms());

    [HttpPost("get")]
    public IActionResult Get([FromBody] GetFormRequest request)
    {
        var form = formService.GetForm(request.Id);
        return form != null ? Ok(form) : NotFound(new { message = "Form not found" });
    }

    [HttpPost("save")]
    public IActionResult Save([FromBody] SaveFormRequest request)
    {
        if (!CanCurrentUserManageForms())
            return Unauthorized(new { message = "You do not have permission to edit forms" });

        var (success, message, id) = formService.SaveForm(request);
        return success ? Ok(new { message, id }) : BadRequest(new { message });
    }

    [HttpPost("delete")]
    public IActionResult Delete([FromBody] DeleteFormRequest request)
    {
        if (!CanCurrentUserManageForms())
            return Unauthorized(new { message = "You do not have permission to delete forms" });

        var (success, message) = formService.DeleteForm(request.Id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    // ── Import / Export (edit permission required) ──

    [HttpPost("export")]
    public IActionResult Export([FromBody] GetFormRequest request)
    {
        if (!CanCurrentUserManageForms())
            return Unauthorized(new { message = "You do not have permission to export forms" });

        var model = formService.ExportForm(request.Id);
        return model != null ? Ok(model) : NotFound(new { message = "Form not found" });
    }

    [HttpPost("import")]
    public IActionResult Import([FromBody] FormExportModel model)
    {
        if (!CanCurrentUserManageForms())
            return Unauthorized(new { message = "You do not have permission to import forms" });

        var (success, message, id) = formService.ImportForm(model);
        return success ? Ok(new { message, id }) : BadRequest(new { message });
    }

    // ── Entries (view: all users, delete: admin only) ──

    [HttpPost("entries")]
    public IActionResult Entries([FromBody] EntryListRequest request)
    {
        var canViewSensitive = CanCurrentUserViewSensitiveData();
        return Ok(formService.GetEntries(
            request.FormId, request.Skip, request.Take, canViewSensitive,
            request.Search, request.DateFrom, request.DateTo));
    }

    [HttpPost("delete-entry")]
    public IActionResult DeleteEntry([FromBody] DeleteFormRequest request)
    {
        if (!CanCurrentUserManageForms())
            return Unauthorized(new { message = "You do not have permission to delete entries" });

        var (success, message) = formService.DeleteEntry(request.Id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    // ── Field types ──

    // Built-in field types. Consuming sites add more via
    // IUmbracoBuilder.AdduTProSimpleFormFieldType(...) — no package edits needed.
    private static readonly SimpleFormFieldType[] BuiltInFieldTypes =
    [
        new("div", "Content Block"),
        new("step", "Form Step"),
        new("text", "Text Input"),
        new("email", "Email"),
        new("tel", "Phone"),
        new("number", "Number"),
        new("textarea", "Text Area"),
        new("select", "Dropdown"),
        new("checkbox", "Checkbox"),
        new("radio", "Radio Buttons"),
        new("file", "File Upload"),
        new("hidden", "Hidden Field"),
        new("date", "Date Picker"),
        new("time", "Time Picker"),
        new("url", "URL"),
        new("password", "Password"),
        new("accept", "Accept / Terms"),
        new("range", "Range Slider"),
        new("color", "Color Picker"),
    ];

    [HttpPost("field-types")]
    public IActionResult FieldTypes()
    {
        // Built-in first, then any consumer-registered custom types. A custom
        // type re-using a built-in key overrides its label in place.
        var ordered = new List<SimpleFormFieldType>(BuiltInFieldTypes);
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < ordered.Count; i++) index[ordered[i].Type] = i;

        foreach (var ft in fieldTypeProviders.SelectMany(p => p.GetFieldTypes()))
        {
            if (string.IsNullOrWhiteSpace(ft.Type)) continue;
            if (index.TryGetValue(ft.Type, out var at)) ordered[at] = ft;
            else { index[ft.Type] = ordered.Count; ordered.Add(ft); }
        }

        return Ok(ordered.Select(t => new { type = t.Type, label = t.Label }));
    }

    // ── Helpers ──

    private bool IsCurrentUserAdmin()
    {
        try
        {
            var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            return user?.IsAdmin() == true;
        }
        catch { return false; }
    }

    // Form management is allowed for admins OR any user whose group grants
    // access to the Settings section (covers custom "admin-like" groups).
    private bool CanCurrentUserManageForms()
    {
        try
        {
            var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            if (user == null) return false;
            if (user.IsAdmin()) return true;
            return user.AllowedSections.Contains(Umbraco.Cms.Core.Constants.Applications.Settings);
        }
        catch { return false; }
    }

    private bool CanCurrentUserViewSensitiveData()
    {
        try
        {
            var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            if (user == null) return false;
            if (user.IsAdmin()) return true;
            return user.Groups.Any(g =>
                string.Equals(g.Alias, "sensitiveData", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }
}
