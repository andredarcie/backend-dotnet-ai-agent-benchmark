using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Validation;

/// <summary>Unlike the built-in [Required], this rejects whitespace-only strings too.</summary>
public sealed class RequiredNotWhitespaceAttribute() : ValidationAttribute("The {0} field is required.")
{
    public override bool IsValid(object? value) => value is string text && !string.IsNullOrWhiteSpace(text);
}
