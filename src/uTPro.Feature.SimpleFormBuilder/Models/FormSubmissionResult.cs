namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// Result returned by an <c>IFormSubmissionHandler</c>. A handler either lets the
/// submission continue (<see cref="Continue"/>) or rejects it with a message and an
/// HTTP status code (<see cref="Reject"/>).
/// </summary>
public sealed class FormSubmissionResult
{
    public bool Success { get; init; }

    /// <summary>Message shown to the submitter when the handler rejects the submission.</summary>
    public string? Message { get; init; }

    /// <summary>HTTP status code to return on rejection (defaults to 400 Bad Request).</summary>
    public int StatusCode { get; init; } = 400;

    /// <summary>A shared "carry on" result.</summary>
    public static FormSubmissionResult Continue { get; } = new() { Success = true };

    /// <summary>Rejects the submission with a message and optional status code.</summary>
    public static FormSubmissionResult Reject(string message, int statusCode = 400)
        => new() { Success = false, Message = message, StatusCode = statusCode };
}
