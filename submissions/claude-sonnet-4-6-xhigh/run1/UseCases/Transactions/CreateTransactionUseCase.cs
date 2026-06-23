using System.Text.Json;
using System.Text.Json.Serialization;
using CreditCardApi.DTOs;
using CreditCardApi.Exceptions;
using CreditCardApi.Kafka;
using CreditCardApi.Models;
using CreditCardApi.Repositories.Interfaces;

namespace CreditCardApi.UseCases.Transactions;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IKafkaProducer _kafkaProducer;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository,
        IKafkaProducer kafkaProducer)
    {
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<Transaction> ExecuteAsync(CreateTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Merchant))
            throw new ValidationException("merchant is required and cannot be empty");

        if (dto.Amount <= 0)
            throw new ValidationException("amount must be > 0");

        var cardExists = await _creditCardRepository.ExistsAsync(dto.CreditCardId);
        if (!cardExists)
            throw new ValidationException($"Credit card with id {dto.CreditCardId} does not exist");

        var transaction = new Transaction
        {
            CreditCardId = dto.CreditCardId,
            Amount = dto.Amount,
            Merchant = dto.Merchant.Trim(),
            Category = dto.Category,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _transactionRepository.AddAsync(transaction);

        var messageValue = JsonSerializer.Serialize(new
        {
            id = created.Id,
            creditCardId = created.CreditCardId,
            amount = created.Amount,
            merchant = created.Merchant,
            category = created.Category,
            createdAt = created.CreatedAt
        }, _jsonOptions);

        await _kafkaProducer.ProduceAsync("transactions", created.Id.ToString(), messageValue);

        return created;
    }
}
