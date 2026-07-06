namespace CreditCardApi.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive card data.
/// </summary>
public interface ICardEncryptionService
{
    /// <summary>
    /// Encrypts plain text (e.g. PAN).
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts cipher text.
    /// </summary>
    string Decrypt(string cipherText);
}
