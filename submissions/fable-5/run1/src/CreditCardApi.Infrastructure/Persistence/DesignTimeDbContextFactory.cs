using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CreditCardApi.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> instantiate the context at design time (adding/scripting migrations)
/// without booting the whole application. Never used at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CreditCardDbContext>
{
    /// <inheritdoc />
    public CreditCardDbContext CreateDbContext(string[] args)
    {
        // No connection is opened for migration scaffolding; a placeholder string suffices.
        var options = new DbContextOptionsBuilder<CreditCardDbContext>()
            .UseNpgsql("Host=localhost;Database=creditcards-design")
            .Options;

        return new CreditCardDbContext(options);
    }
}
