using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Application.Common;

public class PaginationQueryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_ClampsBelowOneToDefault(int requestedPage)
    {
        var query = new PaginationQuery { Page = requestedPage };
        Assert.Equal(PaginationQuery.DefaultPage, query.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageSize_ClampsBelowOneToDefault(int requestedPageSize)
    {
        var query = new PaginationQuery { PageSize = requestedPageSize };
        Assert.Equal(PaginationQuery.DefaultPageSize, query.PageSize);
    }

    [Fact]
    public void PageSize_ClampsAboveMaxToMax()
    {
        var query = new PaginationQuery { PageSize = PaginationQuery.MaxPageSize + 1000 };
        Assert.Equal(PaginationQuery.MaxPageSize, query.PageSize);
    }

    [Fact]
    public void PageSize_WithinBounds_IsUnchanged()
    {
        var query = new PaginationQuery { PageSize = 50 };
        Assert.Equal(50, query.PageSize);
    }
}
