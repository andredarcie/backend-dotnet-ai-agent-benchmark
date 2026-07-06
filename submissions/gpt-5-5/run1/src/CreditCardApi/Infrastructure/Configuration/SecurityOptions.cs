using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Infrastructure.Configuration;

public sealed class SecurityOptions
{
    [Required]
    public string PanEncryptionKey { get; init; } = string.Empty;

    public static bool HasValidKey(SecurityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PanEncryptionKey))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(options.PanEncryptionKey).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
