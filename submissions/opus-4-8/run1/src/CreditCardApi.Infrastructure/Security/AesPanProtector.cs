using System.Security.Cryptography;
using System.Text;
using CreditCardApi.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Security;

/// <summary>
/// Protects the PAN with AES-256-GCM (authenticated encryption). The stored value is
/// <c>base64(nonce || tag || ciphertext)</c>; only the last four digits are kept in clear text.
/// </summary>
public sealed class AesPanProtector : IPanProtector
{
    private const int KeySizeBytes = 32;
    private readonly byte[] _key;

    /// <summary>Creates the protector from the configured key, or an ephemeral key in its absence.</summary>
    public AesPanProtector(IOptions<PanProtectionOptions> options, ILogger<AesPanProtector> logger)
    {
        var configured = options.Value.PanEncryptionKey;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _key = Convert.FromBase64String(configured);
            if (_key.Length != KeySizeBytes)
            {
                throw new InvalidOperationException(
                    $"Security:PanEncryptionKey must be a base64-encoded {KeySizeBytes}-byte key.");
            }
        }
        else
        {
            // No secret in source: generate a per-process key so the app still boots in dev.
            _key = RandomNumberGenerator.GetBytes(KeySizeBytes);
            logger.LogWarning(
                "No PAN encryption key configured; using an ephemeral key. " +
                "Set Security:PanEncryptionKey to retain decryptability across restarts.");
        }
    }

    /// <inheritdoc />
    public ProtectedPan Protect(string pan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pan);

        var digits = new string(pan.Where(char.IsDigit).ToArray());
        var last4 = digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');
        return new ProtectedPan(Encrypt(pan), last4);
    }

    private string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);
        return Convert.ToBase64String(combined);
    }
}
