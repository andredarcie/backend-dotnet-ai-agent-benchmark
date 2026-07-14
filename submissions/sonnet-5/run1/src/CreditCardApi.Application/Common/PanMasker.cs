namespace CreditCardApi.Application.Common;

/// <summary>
/// Masks a PAN for display. The full number is never returned by the API or written to a log -
/// only this masked form (e.g. "**** **** **** 1111") leaves the process.
/// </summary>
public static class PanMasker
{
    public static string Mask(string cardNumber)
    {
        var digits = cardNumber.Where(char.IsDigit).ToArray();
        if (digits.Length < 4)
            return new string('*', digits.Length);

        var last4 = new string(digits[^4..]);
        var maskedGroupCount = (digits.Length - 4 + 3) / 4;
        var maskedGroups = Enumerable.Repeat("****", maskedGroupCount);
        return string.Join(' ', maskedGroups.Append(last4));
    }
}
