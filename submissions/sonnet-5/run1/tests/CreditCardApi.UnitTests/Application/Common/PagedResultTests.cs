using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Application.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 20, 5)]
    public void TotalPages_RoundsUpToTheNearestWholePage(int totalCount, int pageSize, int expectedTotalPages)
    {
        var result = new PagedResult<int>([], totalCount, page: 1, pageSize);
        Assert.Equal(expectedTotalPages, result.TotalPages);
    }

    [Fact]
    public void TotalPages_IsZeroWhenPageSizeIsZero()
    {
        var result = new PagedResult<int>([], totalCount: 10, page: 1, pageSize: 0);
        Assert.Equal(0, result.TotalPages);
    }
}
