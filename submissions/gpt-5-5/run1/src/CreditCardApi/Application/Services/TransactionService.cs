using System.Text.Json;
using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Dtos;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Data;
using CreditCardApi.Data.Entities;
using CreditCardApi.Domain.Entities;
using CreditCardApi.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Application.Services;

public sealed class TransactionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IClock _clock;

    public TransactionService(ApplicationDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<PagedResult<TransactionResponse>> GetPageAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var transactions = _dbContext.Transactions.AsNoTracking().OrderByDescending(transaction => transaction.CreatedAt).ThenBy(transaction => transaction.Id);
        var totalCount = await transactions.CountAsync(cancellationToken);
        var items = await transactions
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(transaction => new TransactionResponse(
                transaction.Id,
                transaction.CreditCardId,
                transaction.Amount,
                transaction.Merchant,
                transaction.Category,
                transaction.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TransactionResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .Where(existing => existing.Id == id)
            .Select(existing => new TransactionResponse(
                existing.Id,
                existing.CreditCardId,
                existing.Amount,
                existing.Merchant,
                existing.Category,
                existing.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return transaction ?? throw new ResourceNotFoundException("Transaction", id);
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CreditCardId, request.Amount, request.Merchant);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var cardExists = await _dbContext.CreditCards
                .AsNoTracking()
                .AnyAsync(card => card.Id == request.CreditCardId, cancellationToken);

            if (!cardExists)
            {
                throw new InvalidRequestException(new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(CreateTransactionRequest.CreditCardId)] = ["Credit card must exist."]
                });
            }

            var transaction = new Transaction(
                request.CreditCardId,
                request.Amount!.Value,
                request.Merchant!,
                request.Category,
                _clock.UtcNow);

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = ToResponse(transaction);
            _dbContext.OutboxMessages.Add(new OutboxMessage(
                Guid.NewGuid(),
                TransactionEventTopics.Transactions,
                response.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonSerializer.Serialize(response, JsonSerializationDefaults.CamelCase),
                _clock.UtcNow));

            await _dbContext.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return response;
        });
    }

    public async Task<TransactionResponse> UpdateAsync(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CreditCardId, request.Amount, request.Merchant);

        var transaction = await _dbContext.Transactions.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Transaction", id);

        var cardExists = await _dbContext.CreditCards
            .AsNoTracking()
            .AnyAsync(card => card.Id == request.CreditCardId, cancellationToken);

        if (!cardExists)
        {
            throw new InvalidRequestException(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpdateTransactionRequest.CreditCardId)] = ["Credit card must exist."]
            });
        }

        transaction.Update(request.CreditCardId, request.Amount!.Value, request.Merchant!, request.Category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(transaction);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("Transaction", id);

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TransactionResponse ToResponse(Transaction transaction) => new(
        transaction.Id,
        transaction.CreditCardId,
        transaction.Amount,
        transaction.Merchant,
        transaction.Category,
        transaction.CreatedAt);

    private static void Validate(int creditCardId, decimal? amount, string? merchant)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(CreateTransactionRequest.CreditCardId)] = creditCardId <= 0 ? ["Credit card id is required."] : [],
            [nameof(CreateTransactionRequest.Amount)] = amount is null or <= 0 ? ["Amount must be greater than zero."] : [],
            [nameof(CreateTransactionRequest.Merchant)] = string.IsNullOrWhiteSpace(merchant) ? ["Merchant is required."] : []
        }
        .Where(pair => pair.Value.Length > 0)
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        if (errors.Count > 0)
        {
            throw new InvalidRequestException(errors);
        }
    }
}



