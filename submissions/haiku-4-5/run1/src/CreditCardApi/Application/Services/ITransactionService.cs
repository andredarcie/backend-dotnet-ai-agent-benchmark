using CreditCardApi.Application.DTOs;

namespace CreditCardApi.Application.Services;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);
}
