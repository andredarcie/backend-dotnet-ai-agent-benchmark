namespace CreditCardApi.Infrastructure.Security;

/// <summary>Configuration for PAN protection. The key is supplied via configuration / environment.</summary>
public sealed class PanProtectionOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Security";

    /// <summary>
    /// Base64-encoded 256-bit (32 byte) key used to encrypt the PAN. Supplied via environment
    /// (for example <c>Security__PanEncryptionKey</c>). Never commit a real key to source control.
    /// </summary>
    public string? PanEncryptionKey { get; set; }
}
