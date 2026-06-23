using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;
using CreditCardApi.Services;

namespace CreditCardApi.UseCases.Transactions;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IKafkaProducerService _kafkaProducer;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        ICreditCardRepository creditCardRepository,
        IKafkaProducerService kafkaProducer)
    {
        _transactionRepository = transactionRepository;
        _creditCardRepository = creditCardRepository;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<(Transaction? Transaction, string? Error)> ExecuteAsync(CreateTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Merchant))
            return (null, "merchant is required");

        if (dto.Amount <= 0)
            return (null, "amount must be > 0");

        var card = await _creditCardRepository.GetByIdAsync(dto.CreditCardId);
        if (card == null)
            return (null, $"creditCardId {dto.CreditCardId} does not exist");

        var transaction = new Transaction
        {
            CreditCardId = dto.CreditCardId,
            Amount = dto.Amount,
            Merchant = dto.Merchant.Trim(),
            Category = dto.Category?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        await _kafkaProducer.PublishTransactionAsync(transaction);

        return (transaction, null);
    }
}
