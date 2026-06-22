using System;
using System.Threading.Tasks;
using Gemini.Data.Repositories;
using Gemini.Messaging;
using Gemini.Models;

namespace Gemini.UseCases;

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

    public async Task<Transaction> ExecuteAsync(Transaction transaction)
    {
        // Validation rules
        if (string.IsNullOrWhiteSpace(transaction.Merchant))
        {
            throw new ArgumentException("Merchant is required.");
        }

        if (transaction.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0.");
        }

        var cardExists = await _creditCardRepository.ExistsAsync(transaction.CreditCardId);
        if (!cardExists)
        {
            throw new ArgumentException("CreditCard does not exist.");
        }

        transaction.CreatedAt = DateTime.UtcNow;

        // Persist to DB
        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        // Publish event to Kafka after persistence
        await _kafkaProducer.PublishTransactionAsync(transaction);

        return transaction;
    }
}
