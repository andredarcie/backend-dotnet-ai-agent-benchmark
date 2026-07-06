using CreditCardApi.Infrastructure.Configuration;
using CreditCardApi.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Tests.Unit;

public sealed class AesCardNumberProtectorTests
{
    private const string Key = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";

    [Fact]
    public void Protect_encrypts_pan_and_never_returns_plain_text()
    {
        var protector = new AesCardNumberProtector(Options.Create(new SecurityOptions { PanEncryptionKey = Key }));

        var cipherText = protector.Protect("4111111111111111");

        cipherText.Should().NotBe("4111111111111111");
        cipherText.Should().NotContain("4111111111111111");
    }

    [Fact]
    public void Last4_returns_only_card_suffix()
    {
        var protector = new AesCardNumberProtector(Options.Create(new SecurityOptions { PanEncryptionKey = Key }));

        protector.Last4("4111 1111-1111 1111").Should().Be("1111");
    }
}
