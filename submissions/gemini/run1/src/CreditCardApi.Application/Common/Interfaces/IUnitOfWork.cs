using System.Threading;
using System.Threading.Tasks;

namespace CreditCardApi.Application.Common.Interfaces;

/// <summary>
/// Unit of work interface for orchestrating database commits.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
