using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CreditCardApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CreditCardApi.Infrastructure.Security;

/// <summary>
/// Infrastructure implementation of the ICardEncryptionService.
/// Uses AES-256-CBC with randomized IVs prepended to ciphertexts.
/// </summary>
public class CardEncryptionService : ICardEncryptionService
{
    private readonly byte[] _key;

    public CardEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"] ?? configuration["ENCRYPTION_KEY"];

        if (string.IsNullOrWhiteSpace(keyString))
        {
            // Fallback for development to ensure it runs out-of-the-box
            // Derive a stable 256-bit key from a fallback passphrase
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes("DevFallbackEncryptionKeyPassphrase_2026"));
        }
        else
        {
            try
            {
                // Try reading as Base64 first
                _key = Convert.FromBase64String(keyString);
            }
            catch (FormatException)
            {
                // If not valid Base64, fallback to SHA256 hash of the raw string to get exactly 32 bytes
                using var sha256 = SHA256.Create();
                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
        }

        if (_key.Length != 32)
        {
            throw new ArgumentException("Encryption key must resolve to exactly 256 bits (32 bytes).");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        var iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        
        // Write IV first
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs, Encoding.UTF8))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        byte[] combinedBytes;
        try
        {
            combinedBytes = Convert.FromBase64String(cipherText);
        }
        catch (FormatException)
        {
            // If it's not base64, it might be unencrypted raw data (e.g. from tests or seeding)
            return cipherText;
        }

        if (combinedBytes.Length < 16)
            throw new CryptographicException("Invalid ciphertext length.");

        var iv = new byte[16];
        var cipherBytes = new byte[combinedBytes.Length - 16];

        Buffer.BlockCopy(combinedBytes, 0, iv, 0, 16);
        Buffer.BlockCopy(combinedBytes, 16, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
