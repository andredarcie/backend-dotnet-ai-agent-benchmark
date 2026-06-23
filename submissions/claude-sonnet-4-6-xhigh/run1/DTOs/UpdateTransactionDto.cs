using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.DTOs;

public class UpdateTransactionDto
{
    [Required]
    public int CreditCardId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "amount must be > 0")]
    public decimal Amount { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "merchant is required")]
    public string Merchant { get; set; } = null!;

    public string? Category { get; set; }
}
