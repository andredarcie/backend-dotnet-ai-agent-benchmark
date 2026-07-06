using CreditCardApi.Application.Abstractions;
using CreditCardApi.Application.Common;
using CreditCardApi.Application.Exceptions;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Domain.Cards;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

public class CreditCardService(
    ICreditCardRepository creditCardRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<CreditCardResponse>> ListAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await creditCardRepository.ListReadOnlyAsync(query.Page, query.PageSize, cancellationToken);
        return new PagedResult<CreditCardResponse>(
            items.Select(CreditCardMapping.ToResponse).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<CreditCardResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardRepository.FindReadOnlyAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit card {id} was not found.");
        return CreditCardMapping.ToResponse(creditCard);
    }

    public async Task<CreditCardResponse> CreateAsync(CreditCardRequest request, CancellationToken cancellationToken)
    {
        var last4 = CardNumberPolicy.TruncateToLast4(request.CardNumber);
        var creditCard = new CreditCard(
            request.CardholderName, last4, request.Brand, request.CreditLimit, timeProvider.GetUtcNowTruncatedToMicroseconds());

        creditCardRepository.Add(creditCard);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreditCardMapping.ToResponse(creditCard);
    }

    public async Task<CreditCardResponse> UpdateAsync(int id, CreditCardRequest request, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit card {id} was not found.");

        var last4 = CardNumberPolicy.TruncateToLast4(request.CardNumber);
        creditCard.UpdateDetails(request.CardholderName, last4, request.Brand, request.CreditLimit);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreditCardMapping.ToResponse(creditCard);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit card {id} was not found.");

        creditCardRepository.Remove(creditCard);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<TransactionResponse>> ListTransactionsAsync(
        int creditCardId, PaginationQuery query, CancellationToken cancellationToken)
    {
        if (!await creditCardRepository.ExistsAsync(creditCardId, cancellationToken))
        {
            throw new NotFoundException($"Credit card {creditCardId} was not found.");
        }

        var (items, totalCount) = await transactionRepository.ListByCreditCardReadOnlyAsync(
            creditCardId, query.Page, query.PageSize, cancellationToken);
        return new PagedResult<TransactionResponse>(
            items.Select(TransactionMapping.ToResponse).ToList(), totalCount, query.Page, query.PageSize);
    }
}
