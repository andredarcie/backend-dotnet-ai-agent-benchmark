using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.DTOs;

/// <summary>
/// DTO for creating a new Transaction.
/// </summary>
public class CreateTransactionDto
{
    [Required(ErrorMessage = "Credit card ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Credit card ID must be a valid positive integer.")]
    public int CreditCardId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Merchant name is required.")]
    public string Merchant { get; set; } = null!;

    public string? Category { get; set; }
}
