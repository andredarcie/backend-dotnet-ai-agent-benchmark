using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Common;

public class PaginationTests
{
    [Theory]
    [InlineData(1, 20, 1, 20)]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-1, 20, 1, 20)]
    [InlineData(1, 0, 1, 20)]
    [InlineData(1, -5, 1, 20)]
    [InlineData(1, 1000, 1, 100)]
    [InlineData(5, 50, 5, 50)]
    public void Normalize_ClampsToSaneBounds(int pageNumber, int pageSize, int expectedPageNumber, int expectedPageSize)
    {
        var (normalizedPageNumber, normalizedPageSize) = Pagination.Normalize(pageNumber, pageSize);

        Assert.Equal(expectedPageNumber, normalizedPageNumber);
        Assert.Equal(expectedPageSize, normalizedPageSize);
    }
}
