using CreditCardApi.Infrastructure.Persistence;

namespace CreditCardApi.Infrastructure.Startup;

public sealed class DatabaseInitializer(AppDbContext context) : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        context.Database.EnsureCreatedAsync(cancellationToken);
}
