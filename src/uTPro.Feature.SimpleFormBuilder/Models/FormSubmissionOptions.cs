namespace uTPro.Feature.SimpleFormBuilder.Models;

/// <summary>
/// Options for the form-submission pipeline, bound from the appsettings section
/// <c>uTPro:Feature:Form</c>.
/// </summary>
public sealed class FormSubmissionOptions
{
    public const string SectionPath = "uTPro:Feature:Form";

    public RateLimitSettings RateLimit { get; set; } = new();

    /// <summary>Per-IP + per-form fixed-window rate limit for public form submissions.</summary>
    public sealed class RateLimitSettings
    {
        /// <summary>When true, submissions are throttled per IP/form. Default true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Max submissions allowed per window per IP/form. Default 5.</summary>
        public int PermitLimit { get; set; } = 5;

        /// <summary>Window length in seconds. Default 60.</summary>
        public int WindowSeconds { get; set; } = 60;
    }
}
