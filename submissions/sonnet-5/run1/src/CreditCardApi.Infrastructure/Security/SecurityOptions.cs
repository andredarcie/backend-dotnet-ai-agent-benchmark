namespace CreditCardApi.Infrastructure.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Base64-encoded 32-byte (AES-256) key used to encrypt the PAN at rest.</summary>
    public required string PanEncryptionKey { get; init; }
}
