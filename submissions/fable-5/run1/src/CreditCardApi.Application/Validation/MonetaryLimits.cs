namespace CreditCardApi.Application.Validation;

/// <summary>Bounds for monetary inputs, aligned with the <c>numeric(18,2)</c> database columns.</summary>
public static class MonetaryLimits
{
    /// <summary>
    /// Largest accepted monetary value. Keeping the API bound below the column's capacity
    /// guarantees a validation failure (400) instead of a database overflow (500).
    /// </summary>
    public const double MaxAmount = 9_999_999_999_999.99;
}
