using CreditCardApi.Application.Common;
using FluentAssertions;
using Xunit;

namespace CreditCardApi.UnitTests.Application;

public sealed class PageRequestTests
{
    [Fact]
    public void Defaults_apply_when_nothing_supplied()
    {
        var page = PageRequest.From(null, null);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(PageRequest.DefaultPageSize);
        page.Skip.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void Page_is_clamped_to_at_least_one(int requested, int expected)
    {
        PageRequest.From(requested, 10).Page.Should().Be(expected);
    }

    [Fact]
    public void Page_size_is_capped_at_the_maximum()
    {
        PageRequest.From(1, 10_000).PageSize.Should().Be(PageRequest.MaxPageSize);
    }

    [Fact]
    public void Skip_is_derived_from_page_and_size()
    {
        PageRequest.From(3, 25).Skip.Should().Be(50);
    }
}
