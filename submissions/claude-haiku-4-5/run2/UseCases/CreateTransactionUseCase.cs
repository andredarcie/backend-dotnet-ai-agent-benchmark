using CreditCardApi.DTOs;
using CreditCardApi.Models;
using CreditCardApi.Repositories;
using CreditCardApi.Services;

namespace CreditCardApi.UseCases;

public class CreateTransactionUseCase(ITransactionRepository transactionRepository, ICreditCardRepository creditCardRepository, IKafkaProducerService kafkaProducerService)
{
    public async Task<Transaction> ExecuteAsync(CreateTransactionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Merchant))
            throw new ArgumentException("Merchant is required");

        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        var creditCard = await creditCardRepository.GetByIdAsync(dto.CreditCardId);
        if (creditCard == null)
            throw new ArgumentException($"CreditCard with id {dto.CreditCardId} not found");

        var transaction = new Transaction
        {
            CreditCardId = dto.CreditCardId,
            Amount = dto.Amount,
            Merchant = dto.Merchant,
            Category = dto.Category,
            CreatedAt = DateTime.UtcNow
        };

        await transactionRepository.AddAsync(transaction);
        await transactionRepository.SaveChangesAsync();

        var transactionDto = new
        {
            transaction.Id,
            transaction.CreditCardId,
            transaction.Amount,
            transaction.Merchant,
            transaction.Category,
            transaction.CreatedAt
        };

        await kafkaProducerService.PublishTransactionAsync(transactionDto);

        return transaction;
    }
}
