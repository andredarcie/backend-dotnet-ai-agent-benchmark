using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Validation;

/// <summary>
/// Validates that a string is present and contains at least one non-whitespace character,
/// i.e. the "required, non-empty" rule of the API contract. <see cref="RequiredAttribute"/>
/// alone accepts whitespace-only values.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class RequiredNotWhitespaceAttribute : ValidationAttribute
{
    /// <summary>Initializes the attribute with the standard error message.</summary>
    public RequiredNotWhitespaceAttribute()
        : base("The {0} field is required and must not be empty.")
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value) => value is string s && !string.IsNullOrWhiteSpace(s);
}
