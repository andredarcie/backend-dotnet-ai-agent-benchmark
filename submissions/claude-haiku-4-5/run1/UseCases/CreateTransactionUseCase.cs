using CreditCardApi.Data.Repositories;
using CreditCardApi.DTOs;
using CreditCardApi.Infrastructure;
using CreditCardApi.Models;
using System.Text.Json;

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

    public async Task<TransactionResponse> ExecuteAsync(CreateTransactionRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        if (string.IsNullOrWhiteSpace(request.Merchant))
            throw new ArgumentException("Merchant is required");

        var creditCard = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
        if (creditCard == null)
            throw new ArgumentException("Credit card not found");

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        var response = new TransactionResponse
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Category = transaction.Category,
            CreatedAt = transaction.CreatedAt
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var kafkaMessage = JsonSerializer.Serialize(response, options);
        await _kafkaProducer.PublishTransactionAsync(transaction.Id.ToString(), kafkaMessage);

        return response;
    }
}
