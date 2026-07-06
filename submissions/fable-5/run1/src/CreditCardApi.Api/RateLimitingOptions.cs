using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Api;

/// <summary>
/// Fixed-window rate limit applied per client IP to the API endpoints, bound from the
/// <c>RateLimiting</c> configuration section. The defaults are deliberately generous: the goal is
/// abuse protection, not throttling legitimate bursts.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per window per client IP.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 1_000;

    /// <summary>Window length in seconds.</summary>
    [Range(1, 3_600)]
    public int WindowSeconds { get; set; } = 10;
}
