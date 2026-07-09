using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>
/// First-in-pipeline gatekeeper that throttles public form submissions using a per-IP +
/// per-form fixed window (configurable via <c>uTPro:Feature:Form:RateLimit</c>). Registered
/// as a singleton because it owns the partitioned rate limiter for the app's lifetime.
/// </summary>
internal sealed class RateLimitSubmissionHandler : IFormSubmissionHandler, IDisposable
{
    public int Order => int.MinValue; // always run first

    private readonly FormSubmissionOptions.RateLimitSettings _settings;
    private readonly PartitionedRateLimiter<string>? _limiter;

    public RateLimitSubmissionHandler(IOptions<FormSubmissionOptions> options)
    {
        _settings = options.Value.RateLimit;

        if (_settings.Enabled)
        {
            _limiter = PartitionedRateLimiter.Create<string, string>(key =>
                RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Math.Max(1, _settings.PermitLimit),
                    Window = TimeSpan.FromSeconds(Math.Max(1, _settings.WindowSeconds)),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        }
    }

    public Task<FormSubmissionResult> HandleAsync(FormSubmissionContext context, CancellationToken cancellationToken)
    {
        if (_limiter == null)
            return Task.FromResult(FormSubmissionResult.Continue);

        // Partition by IP + form alias so a flood on one form doesn't lock out others.
        var key = $"{context.IpAddress ?? "unknown"}|{context.Form.Alias}";
        using var lease = _limiter.AttemptAcquire(key, 1);

        return Task.FromResult(lease.IsAcquired
            ? FormSubmissionResult.Continue
            : FormSubmissionResult.Reject(
                "Too many submissions. Please wait a moment and try again.",
                StatusCodes.Status429TooManyRequests));
    }

    public void Dispose() => _limiter?.Dispose();
}
