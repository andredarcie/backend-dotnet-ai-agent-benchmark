namespace CreditCardApi.Domain.Cards;

/// <summary>
/// PCI DSS Requirement 3 policy for handling a Primary Account Number (PAN): the full number is
/// never persisted or echoed back — only the last 4 digits are kept, and only a masked form is shown.
/// </summary>
public static class CardNumberPolicy
{
    private const int VisibleDigits = 4;

    public static string TruncateToLast4(string rawCardNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCardNumber);

        var digits = new string(rawCardNumber.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length < VisibleDigits)
        {
            throw new ArgumentException(
                $"Card number must contain at least {VisibleDigits} digits.", nameof(rawCardNumber));
        }

        return digits[^VisibleDigits..];
    }

    public static string Mask(string last4) => $"**** **** **** {last4}";
}
