using CreditCardApi.Application.Abstractions;
using CreditCardApi.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CreditCardApi.Infrastructure.Security;

public sealed class AesCardNumberProtector : ICardNumberProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesCardNumberProtector(IOptions<SecurityOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.PanEncryptionKey);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Security:PanEncryptionKey must be a base64-encoded 256-bit key.");
        }
    }

    public string Protect(string cardNumber)
    {
        var normalized = Normalize(cardNumber);
        var plainText = Encoding.UTF8.GetBytes(normalized);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        var protectedPayload = new byte[nonce.Length + tag.Length + cipherText.Length];
        Buffer.BlockCopy(nonce, 0, protectedPayload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, protectedPayload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherText, 0, protectedPayload, nonce.Length + tag.Length, cipherText.Length);

        CryptographicOperations.ZeroMemory(plainText);
        return Convert.ToBase64String(protectedPayload);
    }

    public string Last4(string cardNumber)
    {
        var normalized = Normalize(cardNumber);
        return normalized.Length <= 4 ? normalized : normalized[^4..];
    }

    private static string Normalize(string cardNumber) => cardNumber.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal).Trim();
}
