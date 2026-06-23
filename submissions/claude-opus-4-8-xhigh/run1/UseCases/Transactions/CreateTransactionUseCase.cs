using CreditCardApi.Application;
using CreditCardApi.DTOs;
using CreditCardApi.Messaging;
using CreditCardApi.Models;
using CreditCardApi.Repositories;

namespace CreditCardApi.UseCases.Transactions;

/// <summary>
/// Persists a new transaction and then publishes a <c>transactions</c> Kafka event.
/// The application layer (this use case) — not the controller — orchestrates both
/// persistence and the event publish, and only publishes after a successful save.
/// </summary>
public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ITransactionEventPublisher _eventPublisher;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository,
        ITransactionEventPublisher eventPublisher)
    {
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<TransactionResponse>> ExecuteAsync(CreateTransactionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Merchant))
        {
            return Result<TransactionResponse>.Invalid("merchant is required.");
        }

        if (request.Amount <= 0)
        {
            return Result<TransactionResponse>.Invalid("amount must be greater than 0.");
        }

        if (!await _creditCardRepository.ExistsAsync(request.CreditCardId, ct))
        {
            return Result<TransactionResponse>.Invalid("creditCardId must reference an existing credit card.");
        }

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant.Trim(),
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _transactionRepository.AddAsync(transaction, ct);
        var response = created.ToResponse();

        // Publish only after the transaction is durably persisted.
        await _eventPublisher.PublishTransactionCreatedAsync(response, ct);

        return Result<TransactionResponse>.Success(response);
    }
}
