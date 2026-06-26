using CreditCardApi.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CreditCardApi.UnitTests.Infrastructure;

public sealed class AesPanProtectorTests
{
    private static AesPanProtector NewProtector() =>
        new(Options.Create(new PanProtectionOptions()), NullLogger<AesPanProtector>.Instance);

    [Fact]
    public void Protect_keeps_only_the_last_four_digits_in_clear()
    {
        var result = NewProtector().Protect("4111111111111234");

        result.Last4.Should().Be("1234");
    }

    [Fact]
    public void Protect_does_not_leak_the_pan_in_the_ciphertext()
    {
        var result = NewProtector().Protect("4111111111111234");

        result.Ciphertext.Should().NotBeNullOrEmpty();
        result.Ciphertext.Should().NotContain("4111");
    }

    [Fact]
    public void Protect_uses_a_fresh_nonce_each_time()
    {
        var protector = NewProtector();

        var first = protector.Protect("4111111111111234");
        var second = protector.Protect("4111111111111234");

        first.Ciphertext.Should().NotBe(second.Ciphertext);
    }
}
