using CreditCardApi.Application.Common;

namespace CreditCardApi.UnitTests.Application.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(21, 10, 3)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expected)
    {
        var page = new PagedResult<int>([], totalCount, 1, pageSize);

        Assert.Equal(expected, page.TotalPages);
    }

    [Fact]
    public void Map_ProjectsItemsAndPreservesMetadata()
    {
        var page = new PagedResult<int>([1, 2, 3], 30, 2, 3);

        var mapped = page.Map(i => $"#{i}");

        Assert.Equal(["#1", "#2", "#3"], mapped.Items);
        Assert.Equal(30, mapped.TotalCount);
        Assert.Equal(2, mapped.Page);
        Assert.Equal(3, mapped.PageSize);
    }
}
