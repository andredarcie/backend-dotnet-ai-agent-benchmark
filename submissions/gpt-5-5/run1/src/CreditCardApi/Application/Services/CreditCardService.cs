using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Dtos;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Security;
using CreditCardApi.Data;
using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Application.Services;

public sealed class CreditCardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICardNumberProtector _cardNumberProtector;
    private readonly IClock _clock;

    public CreditCardService(ApplicationDbContext dbContext, ICardNumberProtector cardNumberProtector, IClock clock)
    {
        _dbContext = dbContext;
        _cardNumberProtector = cardNumberProtector;
        _clock = clock;
    }

    public async Task<PagedResult<CreditCardResponse>> GetPageAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var cards = _dbContext.CreditCards.AsNoTracking().OrderBy(card => card.Id);
        var totalCount = await cards.CountAsync(cancellationToken);
        var rows = await cards
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(card => new CreditCardProjection(
                card.Id,
                card.CardholderName,
                card.CardNumberLast4,
                card.Brand,
                card.CreditLimit,
                card.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CreditCardResponse>(
            rows.Select(ToResponse).ToList(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<CreditCardResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.CreditCards
            .AsNoTracking()
            .Where(card => card.Id == id)
            .Select(card => new CreditCardProjection(
                card.Id,
                card.CardholderName,
                card.CardNumberLast4,
                card.Brand,
                card.CreditLimit,
                card.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? throw new ResourceNotFoundException("CreditCard", id) : ToResponse(row);
    }

    public async Task<CreditCardResponse> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CardholderName, request.CardNumber, request.CreditLimit);

        var card = new CreditCard(
            request.CardholderName!,
            _cardNumberProtector.Protect(request.CardNumber!),
            _cardNumberProtector.Last4(request.CardNumber!),
            request.Brand,
            request.CreditLimit!.Value,
            _clock.UtcNow);

        _dbContext.CreditCards.Add(card);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(card);
    }

    public async Task<CreditCardResponse> UpdateAsync(int id, UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        Validate(request.CardholderName, request.CardNumber, request.CreditLimit);

        var card = await _dbContext.CreditCards.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("CreditCard", id);

        card.Update(
            request.CardholderName!,
            _cardNumberProtector.Protect(request.CardNumber!),
            _cardNumberProtector.Last4(request.CardNumber!),
            request.Brand,
            request.CreditLimit!.Value);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(card);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var card = await _dbContext.CreditCards.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException("CreditCard", id);

        _dbContext.CreditCards.Remove(card);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(int id, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CreditCards.AsNoTracking().AnyAsync(card => card.Id == id, cancellationToken);
        if (!exists)
        {
            throw new ResourceNotFoundException("CreditCard", id);
        }

        return await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.CreditCardId == id)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenBy(transaction => transaction.Id)
            .Select(transaction => new TransactionResponse(
                transaction.Id,
                transaction.CreditCardId,
                transaction.Amount,
                transaction.Merchant,
                transaction.Category,
                transaction.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static CreditCardResponse ToResponse(CreditCardProjection projection) => new(
        projection.Id,
        projection.CardholderName,
        CardNumberMask.Mask(projection.CardNumberLast4),
        projection.Brand,
        projection.CreditLimit,
        projection.CreatedAt);

    private static CreditCardResponse ToResponse(CreditCard card) => new(
        card.Id,
        card.CardholderName,
        CardNumberMask.Mask(card.CardNumberLast4),
        card.Brand,
        card.CreditLimit,
        card.CreatedAt);

    private static void Validate(string? cardholderName, string? cardNumber, decimal? creditLimit)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(CreateCreditCardRequest.CardholderName)] = string.IsNullOrWhiteSpace(cardholderName) ? ["Cardholder name is required."] : [],
            [nameof(CreateCreditCardRequest.CardNumber)] = string.IsNullOrWhiteSpace(cardNumber) ? ["Card number is required."] : [],
            [nameof(CreateCreditCardRequest.CreditLimit)] = creditLimit is null or < 0 ? ["Credit limit must be greater than or equal to zero."] : []
        }
        .Where(pair => pair.Value.Length > 0)
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        if (errors.Count > 0)
        {
            throw new InvalidRequestException(errors);
        }
    }

    private sealed record CreditCardProjection(
        int Id,
        string CardholderName,
        string CardNumberLast4,
        string? Brand,
        decimal CreditLimit,
        DateTimeOffset CreatedAt);
}
