namespace CreditCardApi.Application.Transactions;

/// <summary>
/// A transaction as returned by the API. Also the payload of the <c>transactions</c> Kafka event.
/// </summary>
/// <param name="Id">Unique identifier of the transaction.</param>
/// <param name="CreditCardId">Identifier of the card that was charged.</param>
/// <param name="Amount">Amount charged.</param>
/// <param name="Merchant">Merchant where the purchase was made.</param>
/// <param name="Category">Spending category. May be <see langword="null"/>.</param>
/// <param name="CreatedAt">UTC timestamp assigned by the server on creation.</param>
public sealed record TransactionResponse(
    int Id,
    int CreditCardId,
    decimal Amount,
    string Merchant,
    string? Category,
    DateTime CreatedAt);
