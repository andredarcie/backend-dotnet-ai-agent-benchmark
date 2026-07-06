namespace CreditCardApi.Api;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 1000;

    public int WindowSeconds { get; set; } = 10;
}
