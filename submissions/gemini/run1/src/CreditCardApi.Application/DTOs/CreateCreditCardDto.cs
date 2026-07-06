using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.DTOs;

/// <summary>
/// DTO for creating a new Credit Card.
/// </summary>
public class CreateCreditCardDto
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Cardholder name is required.")]
    public string CardholderName { get; set; } = null!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Card number is required.")]
    [MinLength(8, ErrorMessage = "Card number must be at least 8 characters long.")]
    public string CardNumber { get; set; } = null!;

    public string? Brand { get; set; }

    [Required(ErrorMessage = "Credit limit is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Credit limit must be greater than or equal to 0.")]
    public decimal CreditLimit { get; set; }
}
