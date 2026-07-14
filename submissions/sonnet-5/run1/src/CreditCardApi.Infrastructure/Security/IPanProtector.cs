namespace CreditCardApi.Infrastructure.Security;

/// <summary>Encrypts/decrypts the credit card PAN for storage. The plaintext PAN never leaves this boundary.</summary>
public interface IPanProtector
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
