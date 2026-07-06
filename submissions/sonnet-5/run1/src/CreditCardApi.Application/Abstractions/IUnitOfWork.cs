namespace CreditCardApi.Application.Abstractions;

/// <summary>Commits everything staged on repositories (and any staged outbox message) as one atomic unit.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
