namespace CreditCardApi.Application.Abstractions;

/// <summary>
/// Protects a Primary Account Number (PAN) so clear text is never persisted or logged.
/// Implementations encrypt the PAN and expose only the last four digits for display.
/// </summary>
public interface IPanProtector
{
    /// <summary>Encrypts <paramref name="pan"/> and extracts its last four digits.</summary>
    ProtectedPan Protect(string pan);
}

/// <summary>The result of protecting a PAN: ciphertext to store, and the last four digits to display.</summary>
/// <param name="Ciphertext">Reversibly encrypted PAN (safe to store).</param>
/// <param name="Last4">The last four digits of the PAN (safe to display).</param>
public readonly record struct ProtectedPan(string Ciphertext, string Last4);
