using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Mapping;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

/// <summary>Use cases for managing credit cards. Controllers depend on this, not on EF Core.</summary>
public sealed class CreditCardService
{
    private readonly ICreditCardRepository _cards;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPanProtector _panProtector;
    private readonly IClock _clock;

    /// <summary>Creates the service with its collaborators.</summary>
    public CreditCardService(
        ICreditCardRepository cards,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork,
        IPanProtector panProtector,
        IClock clock)
    {
        _cards = cards;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _panProtector = panProtector;
        _clock = clock;
    }

    /// <summary>Returns a page of cards.</summary>
    public async Task<PagedResult<CreditCardResponse>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var total = await _cards.CountAsync(cancellationToken);
        var items = await _cards.ListAsync(page.Skip, page.PageSize, cancellationToken);
        var responses = items.Select(c => c.ToResponse()).ToList();
        return new PagedResult<CreditCardResponse>(responses, page.Page, page.PageSize, total);
    }

    /// <summary>Returns a single card, or throws <see cref="NotFoundException"/>.</summary>
    public async Task<CreditCardResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(CreditCard), id);
        return card.ToResponse();
    }

    /// <summary>Creates a new card, encrypting the PAN before it is persisted.</summary>
    public async Task<CreditCardResponse> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var protectedPan = _panProtector.Protect(request.CardNumber);
        var card = CreditCard.Create(
            request.CardholderName,
            protectedPan.Ciphertext,
            protectedPan.Last4,
            request.Brand,
            request.CreditLimit,
            _clock.UtcNow);

        _cards.Add(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return card.ToResponse();
    }

    /// <summary>Updates an existing card, or throws <see cref="NotFoundException"/>.</summary>
    public async Task UpdateAsync(int id, UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(CreditCard), id);
        card.Update(request.CardholderName, request.Brand, request.CreditLimit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes a card and its transactions (cascade), or throws <see cref="NotFoundException"/>.</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var card = await _cards.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(CreditCard), id);
        _cards.Remove(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns a page of transactions for a card, or throws if the card does not exist.</summary>
    public async Task<PagedResult<TransactionResponse>> ListTransactionsAsync(int cardId, PageRequest page, CancellationToken cancellationToken)
    {
        if (!await _cards.ExistsAsync(cardId, cancellationToken))
        {
            throw new NotFoundException(nameof(CreditCard), cardId);
        }

        var total = await _transactions.CountByCardAsync(cardId, cancellationToken);
        var items = await _transactions.ListByCardAsync(cardId, page.Skip, page.PageSize, cancellationToken);
        var responses = items.Select(t => t.ToResponse()).ToList();
        return new PagedResult<TransactionResponse>(responses, page.Page, page.PageSize, total);
    }
}
