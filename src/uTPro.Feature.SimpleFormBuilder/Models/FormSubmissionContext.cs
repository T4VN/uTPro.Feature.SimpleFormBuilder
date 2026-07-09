using Microsoft.AspNetCore.Http;

namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// The parsed, in-flight state of a form submission handed to each
/// <c>IFormSubmissionHandler</c> before the entry is stored. Handlers may inspect the
/// resolved form and the submitted values (e.g. to verify a captcha, run custom field
/// logic, or apply rate limiting) and short-circuit the submission by returning a
/// failing <see cref="FormSubmissionResult"/>.
/// </summary>
public sealed class FormSubmissionContext
{
    /// <summary>The resolved, enabled form the submission targets.</summary>
    public required FormViewModel Form { get; init; }

    /// <summary>Submitted field values keyed by field name.</summary>
    public required IReadOnlyDictionary<string, string> Data { get; init; }

    /// <summary>Remote IP address of the submitter, if known.</summary>
    public string? IpAddress { get; init; }

    /// <summary>User-Agent header of the submitter, if known.</summary>
    public string? UserAgent { get; init; }

    /// <summary>The current request context (for handlers that need headers/connection info).</summary>
    public HttpContext? HttpContext { get; init; }
}
