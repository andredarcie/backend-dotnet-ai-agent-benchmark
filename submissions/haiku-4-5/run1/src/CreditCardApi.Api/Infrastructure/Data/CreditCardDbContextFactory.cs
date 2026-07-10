using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CreditCardApi.Api.Infrastructure.Data;

public class CreditCardDbContextFactory : IDesignTimeDbContextFactory<CreditCardDbContext>
{
    public CreditCardDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CreditCardDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=creditcard;Username=postgres;Password=postgres");

        return new CreditCardDbContext(optionsBuilder.Options);
    }
}
