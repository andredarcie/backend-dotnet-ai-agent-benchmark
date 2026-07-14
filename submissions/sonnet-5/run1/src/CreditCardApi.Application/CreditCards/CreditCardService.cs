using CreditCardApi.Application.Common;
using CreditCardApi.Application.Common.Exceptions;
using CreditCardApi.Application.CreditCards.Dtos;
using CreditCardApi.Application.Transactions;
using CreditCardApi.Application.Transactions.Dtos;
using CreditCardApi.Domain.Entities;

namespace CreditCardApi.Application.CreditCards;

public sealed class CreditCardService(
    ICreditCardRepository creditCardRepository,
    ITransactionRepository transactionRepository,
    TimeProvider timeProvider) : ICreditCardService
{
    public async Task<CreditCardResponse> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CardholderName))
        {
            errors["cardholderName"] = ["Cardholder name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            errors["cardNumber"] = ["Card number is required."];
        }

        if (request.CreditLimit < 0)
        {
            errors["creditLimit"] = ["Credit limit must be greater than or equal to 0."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var creditCard = new CreditCard
        {
            CardholderName = request.CardholderName,
            CardNumber = request.CardNumber,
            Brand = request.Brand,
            CreditLimit = request.CreditLimit,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime.TruncateToMicroseconds(),
        };

        creditCardRepository.Add(creditCard);
        await creditCardRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(creditCard);
    }

    public async Task<CreditCardResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var creditCard = await creditCardRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit card {id} was not found.");

        return ToResponse(creditCard);
    }

    public async Task<IReadOnlyList<CreditCardResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var (normalizedPageNumber, normalizedPageSize) = Pagination.Normalize(pageNumber, pageSize);
        var creditCards = await creditCardRepository.GetPagedAsync(normalizedPageNumber, normalizedPageSize, cancellationToken);
        return [.. creditCards.Select(ToResponse)];
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsForCardAsync(int creditCardId, CancellationToken cancellationToken)
    {
        if (!await creditCardRepository.ExistsAsync(creditCardId, cancellationToken))
        {
            throw new NotFoundException($"Credit card {creditCardId} was not found.");
        }

        var transactions = await transactionRepository.GetByCreditCardIdAsync(creditCardId, cancellationToken);
        return [.. transactions.Select(TransactionService.ToResponse)];
    }

    private static CreditCardResponse ToResponse(CreditCard creditCard) => new(
        creditCard.Id,
        creditCard.CardholderName,
        PanMasker.Mask(creditCard.CardNumber),
        creditCard.Brand,
        creditCard.CreditLimit,
        creditCard.CreatedAt);
}
