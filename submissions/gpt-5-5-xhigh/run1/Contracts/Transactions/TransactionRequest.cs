namespace CreditCardApi.Contracts.Transactions;

public sealed class TransactionRequest
{
    public int CreditCardId { get; set; }
    public decimal Amount { get; set; }
    public string? Merchant { get; set; }
    public string? Category { get; set; }
}
