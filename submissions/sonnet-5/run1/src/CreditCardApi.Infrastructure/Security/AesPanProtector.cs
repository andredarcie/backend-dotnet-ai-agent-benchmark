using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Security;

/// <summary>
/// AES-256-GCM authenticated encryption for the PAN. Storage format is
/// base64(nonce[12] || ciphertext || tag[16]) - a fresh random nonce per call, so encrypting the
/// same PAN twice never produces the same ciphertext.
/// </summary>
public sealed class AesPanProtector : IPanProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesPanProtector(IOptions<SecurityOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.PanEncryptionKey);
        if (_key.Length is not 32)
        {
            throw new InvalidOperationException(
                "Security:PanEncryptionKey must be a base64-encoded 32-byte (AES-256) key.");
        }
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var data = Convert.FromBase64String(cipherText);
        var cipherSize = data.Length - NonceSize - TagSize;

        var nonce = data.AsSpan(0, NonceSize);
        var cipherBytes = data.AsSpan(NonceSize, cipherSize);
        var tag = data.AsSpan(NonceSize + cipherSize, TagSize);

        var plainBytes = new byte[cipherSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
