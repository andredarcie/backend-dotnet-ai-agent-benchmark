using System;

namespace CreditCardApi.Application.Common.Security;

/// <summary>
/// Helper to mask sensitive Primary Account Numbers (PANs).
/// </summary>
public static class CardNumberMasker
{
    /// <summary>
    /// Masks a credit card number, leaving only the first 6 and last 4 digits visible if long enough,
    /// or only the last 4 digits visible.
    /// </summary>
    public static string Mask(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return string.Empty;

        // Clean spaces and hyphens
        var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

        if (cleanNumber.Length < 10)
        {
            // For short numbers, just mask everything except the last 2 digits
            if (cleanNumber.Length <= 2)
                return new string('*', cleanNumber.Length);
            return new string('*', cleanNumber.Length - 2) + cleanNumber[^2..];
        }

        // Standard masking: first 6 digits and last 4 digits are kept, middle is masked
        var first6 = cleanNumber[..6];
        var last4 = cleanNumber[^4..];
        var maskedLength = cleanNumber.Length - 10;
        var middleMask = new string('*', maskedLength);

        return $"{first6}{middleMask}{last4}";
    }
}
