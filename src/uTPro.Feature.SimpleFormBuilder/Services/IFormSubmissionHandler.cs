using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>
/// A pluggable step in the form-submission pipeline, run (ordered by <see cref="Order"/>)
/// after the request is parsed but BEFORE the entry is stored. Register implementations in
/// DI as <c>IFormSubmissionHandler</c>; the submit endpoint resolves them all and executes
/// them in order, stopping at the first one that rejects.
///
/// This is the extension point for custom field types (e.g. Cloudflare Turnstile),
/// anti-spam / rate limiting, or any per-submission validation — without touching the
/// core service or the HTTP pipeline.
/// </summary>
public interface IFormSubmissionHandler
{
    /// <summary>Execution order (ascending). Lower runs first; use a low value for gatekeepers
    /// like rate limiting and higher values for field-specific validation.</summary>
    int Order { get; }

    Task<FormSubmissionResult> HandleAsync(FormSubmissionContext context, CancellationToken cancellationToken);
}
