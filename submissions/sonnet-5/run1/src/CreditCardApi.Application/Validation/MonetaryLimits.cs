namespace CreditCardApi.Application.Validation;

/// <summary>
/// Bounds for monetary request fields. The database column is <c>numeric(18,2)</c>; keeping validated
/// input well under its ceiling guarantees a request can never overflow the column.
/// </summary>
public static class MonetaryLimits
{
    public const string MinPositiveAmount = "0.01";
    public const string MaxAmount = "999999999999.99";
}
