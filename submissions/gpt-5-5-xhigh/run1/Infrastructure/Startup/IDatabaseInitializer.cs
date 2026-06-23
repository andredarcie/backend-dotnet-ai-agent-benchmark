namespace CreditCardApi.Infrastructure.Startup;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
