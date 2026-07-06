using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` construct a context without spinning up the full host.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CreditCardDbContext>
{
    public CreditCardDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=creditcards;Username=creditcards;Password=local-dev-only";

        var optionsBuilder = new DbContextOptionsBuilder<CreditCardDbContext>();
        optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        return new CreditCardDbContext(optionsBuilder.Options);
    }
}
