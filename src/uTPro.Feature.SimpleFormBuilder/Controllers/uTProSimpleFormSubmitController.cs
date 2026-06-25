using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.Controllers;

[ApiController]
[Route("api/utpro/simple-form")]
public class uTProSimpleFormSubmitController(IuTProSimpleFormService formService) : ControllerBase
{
    [HttpPost("submit")]
    public IActionResult Submit([FromBody] SubmitFormRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = HttpContext.Request.Headers.UserAgent.ToString();
        var (success, message) = formService.SubmitForm(request.Alias, request.Data, ip, ua);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("render/{alias}")]
    public IActionResult RenderForm(string alias)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled) return NotFound(new { message = "Form not found" });
        if (!form.EnableRenderApi) return NotFound(new { message = "Render API is disabled for this form" });
        return Ok(form);
    }

    [HttpGet("entries/{alias}")]
    public IActionResult PublicEntries(string alias, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled) return NotFound(new { message = "Form not found" });
        if (!form.EnableEntriesApi) return NotFound(new { message = "Entries API is disabled for this form" });
        // Public API: sensitive data is always masked
        var result = formService.GetEntries(form.Id, skip, take, canViewSensitive: false);
        return Ok(result);
    }
}
