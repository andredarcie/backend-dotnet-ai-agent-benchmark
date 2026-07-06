using CreditCardApi.Application.Validation;

namespace CreditCardApi.UnitTests.Application.Validation;

public class RequiredNotWhitespaceAttributeTests
{
    private readonly RequiredNotWhitespaceAttribute _attribute = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void IsValid_RejectsNullEmptyAndWhitespace(string? value)
    {
        Assert.False(_attribute.IsValid(value));
    }

    [Theory]
    [InlineData("Ada Lovelace")]
    [InlineData("x")]
    [InlineData(" x ")]
    public void IsValid_AcceptsNonBlankStrings(string value)
    {
        Assert.True(_attribute.IsValid(value));
    }

    [Fact]
    public void IsValid_RejectsNonStringValues()
    {
        Assert.False(_attribute.IsValid(123));
    }
}
