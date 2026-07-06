using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotBlankAttribute : ValidationAttribute
{
    public NotBlankAttribute()
        : base("The {0} field is required.")
    {
    }

    public override bool IsValid(object? value) => value is string text && !string.IsNullOrWhiteSpace(text);
}
