using System.ComponentModel.DataAnnotations;

namespace CreditCardApi.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NonNegativeDecimalAttribute : ValidationAttribute
{
    public NonNegativeDecimalAttribute()
        : base("The {0} field must be greater than or equal to zero.")
    {
    }

    public override bool IsValid(object? value) => value is decimal amount && amount >= 0;
}
