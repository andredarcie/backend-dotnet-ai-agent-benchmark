using CreditCardApi.Infrastructure.Persistence;

namespace CreditCardApi.UnitTests.Infrastructure;

public class AppDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_BuildsAUsableContextWithoutOpeningAConnection()
    {
        var sut = new AppDbContextFactory();

        using var dbContext = sut.CreateDbContext([]);

        Assert.NotNull(dbContext);
        Assert.NotNull(dbContext.Model.FindEntityType(typeof(CreditCardApi.Domain.Entities.CreditCard)));
    }
}
