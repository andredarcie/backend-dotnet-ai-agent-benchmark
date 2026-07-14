using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` construct an <see cref="AppDbContext"/> without booting the
/// full DI container. The placeholder encryption key below is never used outside migration
/// generation - the running app always reads Security__PanEncryptionKey from the environment.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeOnlyKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=creditcardapi;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var panProtector = new AesPanProtector(Options.Create(new SecurityOptions { PanEncryptionKey = DesignTimeOnlyKey }));

        return new AppDbContext(optionsBuilder.Options, panProtector);
    }
}
