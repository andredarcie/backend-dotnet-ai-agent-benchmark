using CreditCardApi.Application.DTOs;
using CreditCardApi.Application.Repositories;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ApplicationDbContext context,
        ITransactionRepository transactionRepository,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var creditCard = await _context.CreditCards.FirstOrDefaultAsync(
                c => c.Id == request.CreditCardId, cancellationToken);

            if (creditCard == null)
            {
                throw new InvalidOperationException($"Credit card with ID {request.CreditCardId} not found");
            }

            var newTransaction = new Transaction
            {
                CreditCardId = request.CreditCardId,
                Amount = request.Amount,
                Merchant = request.Merchant,
                Category = request.Category,
                CreatedAt = DateTime.UtcNow
            };

            var createdTransaction = await _transactionRepository.AddAsync(newTransaction, cancellationToken);

            var payload = JsonSerializer.Serialize(new TransactionDto(
                createdTransaction.Id,
                createdTransaction.CreditCardId,
                createdTransaction.Amount,
                createdTransaction.Merchant,
                createdTransaction.Category,
                createdTransaction.CreatedAt));

            var outboxEvent = new OutboxEvent
            {
                Topic = "transactions",
                Key = createdTransaction.Id.ToString(),
                Payload = payload,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            await _context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Transaction {TransactionId} created and published to outbox",
                createdTransaction.Id);

            return new TransactionDto(
                createdTransaction.Id,
                createdTransaction.CreditCardId,
                createdTransaction.Amount,
                createdTransaction.Merchant,
                createdTransaction.Category,
                createdTransaction.CreatedAt);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating transaction");
            throw;
        }
    }
}
