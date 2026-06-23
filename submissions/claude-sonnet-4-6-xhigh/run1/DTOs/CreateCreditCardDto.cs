using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.DTOs;

public class CreateCreditCardDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "cardholderName is required")]
    public string CardholderName { get; set; } = null!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "cardNumber is required")]
    public string CardNumber { get; set; } = null!;

    public string? Brand { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "creditLimit must be >= 0")]
    public decimal CreditLimit { get; set; }
}
