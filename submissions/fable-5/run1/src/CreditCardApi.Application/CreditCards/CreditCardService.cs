using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Cards;
using CreditCardApi.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Use cases for managing credit cards.</summary>
/// <remarks>
/// Request DTOs are assumed to have passed model validation at the API boundary; this service
/// enforces the rules that need data access (existence checks) and normalizes values for storage.
/// </remarks>
public sealed class CreditCardService
{
    private readonly ICreditCardRepository _cards;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<CreditCardService> _logger;

    /// <summary>Creates the service with its collaborators.</summary>
    public CreditCardService(
        ICreditCardRepository cards,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<CreditCardService> logger)
    {
        _cards = cards;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Returns one page of credit cards.</summary>
    public async Task<PagedResult<CreditCardResponse>> GetPageAsync(PaginationQuery page, CancellationToken cancellationToken)
    {
        var result = await _cards.GetPageAsync(page, cancellationToken);
        return result.Map(card => card.ToResponse());
    }

    /// <summary>Returns a single credit card, or <see langword="null"/> if it does not exist.</summary>
    public async Task<CreditCardResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var card = await _cards.GetAsync(id, cancellationToken);
        return card?.ToResponse();
    }

    /// <summary>Creates a credit card and returns it, retaining only the last four digits of the PAN.</summary>
    public async Task<CreditCardResponse> CreateAsync(CreditCardRequest request, CancellationToken cancellationToken)
    {
        var card = new CreditCard
        {
            CardholderName = request.CardholderName!.Trim(),
            CardNumberLast4 = CardNumber.ToLast4(request.CardNumber!),
            Brand = NormalizeBrand(request.Brand),
            CreditLimit = decimal.Round(request.CreditLimit!.Value, 2, MidpointRounding.AwayFromZero),
            CreatedAt = _clock.GetUtcNowForStorage(),
        };

        _cards.Add(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created credit card {CreditCardId}", card.Id);
        return card.ToResponse();
    }

    /// <summary>Replaces a credit card's data. Returns <see langword="null"/> if it does not exist.</summary>
    public async Task<CreditCardResponse?> UpdateAsync(int id, CreditCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetForUpdateAsync(id, cancellationToken);
        if (card is null)
        {
            return null;
        }

        card.CardholderName = request.CardholderName!.Trim();
        card.CardNumberLast4 = CardNumber.ToLast4(request.CardNumber!);
        card.Brand = NormalizeBrand(request.Brand);
        card.CreditLimit = decimal.Round(request.CreditLimit!.Value, 2, MidpointRounding.AwayFromZero);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated credit card {CreditCardId}", card.Id);
        return card.ToResponse();
    }

    /// <summary>Deletes a credit card and its transactions. Returns <see langword="false"/> if it does not exist.</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _cards.DeleteAsync(id, cancellationToken);
        if (deleted)
        {
            _logger.LogInformation("Deleted credit card {CreditCardId}", id);
        }

        return deleted;
    }

    /// <summary>
    /// Returns one page of a card's transactions, or <see langword="null"/> if the card does not exist.
    /// </summary>
    public async Task<PagedResult<TransactionResponse>?> GetTransactionsAsync(
        int creditCardId,
        PaginationQuery page,
        CancellationToken cancellationToken)
    {
        if (!await _cards.ExistsAsync(creditCardId, cancellationToken))
        {
            return null;
        }

        var result = await _transactions.GetPageForCardAsync(creditCardId, page, cancellationToken);
        return result.Map(transaction => transaction.ToResponse());
    }

    private static string? NormalizeBrand(string? brand) =>
        string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
}
