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
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor) : ManagementApiControllerBase
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

    [HttpPost("field-types")]
    public IActionResult FieldTypes() => Ok(new[]
    {
        new { type = "div", label = "Content Block" },
        new { type = "step", label = "Form Step" },
        new { type = "text", label = "Text Input" },
        new { type = "email", label = "Email" },
        new { type = "tel", label = "Phone" },
        new { type = "number", label = "Number" },
        new { type = "textarea", label = "Text Area" },
        new { type = "select", label = "Dropdown" },
        new { type = "checkbox", label = "Checkbox" },
        new { type = "radio", label = "Radio Buttons" },
        new { type = "file", label = "File Upload" },
        new { type = "hidden", label = "Hidden Field" },
        new { type = "date", label = "Date Picker" },
        new { type = "time", label = "Time Picker" },
        new { type = "url", label = "URL" },
        new { type = "password", label = "Password" },
        new { type = "accept", label = "Accept / Terms" },
        new { type = "range", label = "Range Slider" },
        new { type = "color", label = "Color Picker" },
    });

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
