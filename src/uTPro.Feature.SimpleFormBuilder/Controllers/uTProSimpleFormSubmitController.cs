using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.Controllers;

[ApiController]
[Route("api/utpro/simple-form")]
public class uTProSimpleFormSubmitController(IuTProSimpleFormService formService) : ControllerBase
{
    // Client names each uploaded file "file:{fieldName}" so we can map it back to its field.
    private const string FileFieldPrefix = "file:";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Handles a form submission. Accepts either JSON (<see cref="SubmitFormRequest"/>) for
    /// API consumers without files, or multipart/form-data — carrying an "alias" field, a
    /// "data" field (JSON of the field values) and any file inputs named "file:{fieldName}".
    /// Uploaded files travel with the submission so they're only persisted once the entry is
    /// stored; a failed or abandoned submit never leaves orphaned files.
    /// </summary>
    [HttpPost("submit")]
    [RequestSizeLimit(104_857_600)] // 100 MB hard cap; per-field maxSize is enforced in the service
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    public async Task<IActionResult> Submit()
    {
        string alias;
        Dictionary<string, string> data;
        var files = new List<FormFileUpload>();

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            alias = form["alias"].ToString();
            var dataJson = form["data"].ToString();
            data = string.IsNullOrEmpty(dataJson)
                ? new()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(dataJson, JsonOpts) ?? new();

            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;
                var fieldName = file.Name.StartsWith(FileFieldPrefix, StringComparison.Ordinal)
                    ? file.Name[FileFieldPrefix.Length..]
                    : file.Name;
                files.Add(new FormFileUpload(fieldName, file));
            }
        }
        else
        {
            SubmitFormRequest? request;
            try { request = await JsonSerializer.DeserializeAsync<SubmitFormRequest>(Request.Body, JsonOpts); }
            catch { return BadRequest(new { message = "Invalid request" }); }
            if (request == null) return BadRequest(new { message = "Invalid request" });
            alias = request.Alias;
            data = request.Data ?? new();
        }

        if (string.IsNullOrEmpty(alias))
            return BadRequest(new { message = "Missing form alias" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = HttpContext.Request.Headers.UserAgent.ToString();
        var (success, message) = formService.SubmitForm(alias, data, files, ip, ua);
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
