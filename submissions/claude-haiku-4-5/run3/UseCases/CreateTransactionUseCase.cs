using System.Text.Json;
using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;
using CreditCardApi.Services;

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
        if (string.IsNullOrWhiteSpace(request.Merchant))
            throw new ArgumentException("Merchant is required");

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        var creditCard = await _creditCardRepository.GetByIdAsync(request.CreditCardId);
        if (creditCard == null)
            throw new ArgumentException("CreditCard not found");

        var transaction = new Transaction
        {
            CreditCardId = request.CreditCardId,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _transactionRepository.AddAsync(transaction);

        var response = new TransactionResponse
        {
            Id = created.Id,
            CreditCardId = created.CreditCardId,
            Amount = created.Amount,
            Merchant = created.Merchant,
            Category = created.Category,
            CreatedAt = created.CreatedAt
        };

        var message = JsonSerializer.Serialize(response);
        await _kafkaProducer.PublishTransactionAsync(message, created.Id.ToString());

        return response;
    }
}
