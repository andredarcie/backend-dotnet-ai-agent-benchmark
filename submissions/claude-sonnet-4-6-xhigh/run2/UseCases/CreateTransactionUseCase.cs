using System.Text.Json;
using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Infrastructure;
using CreditCardApi.Models;

namespace CreditCardApi.UseCases;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IKafkaProducer _kafkaProducer;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository,
        IKafkaProducer kafkaProducer)
    {
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<(Transaction? Transaction, string? Error)> ExecuteAsync(CreateTransactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Merchant))
            return (null, "merchant is required.");

        if (request.Amount <= 0)
            return (null, "amount must be > 0.");

        var card = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
        if (card is null)
            return (null, $"creditCardId {request.CreditCardId} does not exist.");

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);

        var payload = JsonSerializer.Serialize(new
        {
            id = transaction.Id,
            creditCardId = transaction.CreditCardId,
            amount = transaction.Amount,
            merchant = transaction.Merchant,
            category = transaction.Category,
            createdAt = transaction.CreatedAt
        });

        await _kafkaProducer.PublishAsync("transactions", transaction.Id.ToString(), payload);

        return (transaction, null);
    }
}
