using CreditCardApi.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CreditCardApi.UnitTests.Infrastructure;

public class AesPanProtectorTests
{
    // Test fixture, not a secret - fixed so assertions are deterministic.
    private const string ValidBase64Key32Bytes = "uO+9Anw4AgetgEyDt//K7i+gwsVA0Af29+vx/QAt+/A="; // gitleaks:allow

    private static AesPanProtector CreateSut(string key = ValidBase64Key32Bytes) =>
        new(Options.Create(new SecurityOptions { PanEncryptionKey = key }));

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTripsTheOriginalValue()
    {
        var sut = CreateSut();
        const string pan = "4111111111111111";

        var cipherText = sut.Encrypt(pan);
        var decrypted = sut.Decrypt(cipherText);

        Assert.Equal(pan, decrypted);
    }

    [Fact]
    public void Encrypt_NeverContainsThePlaintextPan()
    {
        var sut = CreateSut();
        const string pan = "4111111111111111";

        var cipherText = sut.Encrypt(pan);

        Assert.DoesNotContain(pan, cipherText, StringComparison.Ordinal);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextForTheSamePanEachCall()
    {
        var sut = CreateSut();
        const string pan = "4111111111111111";

        var first = sut.Encrypt(pan);
        var second = sut.Encrypt(pan);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("dG9vc2hvcnQ=")] // valid base64, but not 32 bytes
    [InlineData("not-valid-base64!!")]
    public void Constructor_WithInvalidKey_Throws(string invalidKey)
    {
        Assert.ThrowsAny<Exception>(() => CreateSut(invalidKey));
    }
}
