namespace CreditCardApi.Domain.Cards;

/// <summary>
/// PAN (primary account number) handling rules.
/// </summary>
/// <remarks>
/// The full PAN is treated as write-only input: it is truncated to its last four digits at the
/// service boundary and the remainder is discarded, never persisted, logged or echoed back.
/// Responses expose only the masked form produced by <see cref="Mask"/>.
/// </remarks>
public static class CardNumber
{
    private const int VisibleDigits = 4;

    /// <summary>
    /// Extracts the only part of a PAN that may be retained: its last four digits.
    /// Non-digit separators (spaces, dashes) are ignored.
    /// </summary>
    /// <param name="pan">The raw card number as supplied by the client.</param>
    /// <returns>The trailing digits of the PAN (at most four).</returns>
    public static string ToLast4(string pan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pan);

        var digits = string.Concat(pan.Where(char.IsAsciiDigit));
        var visible = digits.Length > 0 ? digits : pan.Trim();
        return visible.Length <= VisibleDigits ? visible : visible[^VisibleDigits..];
    }

    /// <summary>
    /// Formats the retained digits for presentation, e.g. <c>**** **** **** 1234</c>.
    /// </summary>
    /// <param name="last4">The digits returned by <see cref="ToLast4"/>.</param>
    /// <returns>The masked card number safe to display and serialize.</returns>
    public static string Mask(string last4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(last4);
        return $"**** **** **** {last4}";
    }
}
