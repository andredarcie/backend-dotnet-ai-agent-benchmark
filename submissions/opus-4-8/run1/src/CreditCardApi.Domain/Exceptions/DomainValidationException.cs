namespace CreditCardApi.Domain.Exceptions;

/// <summary>
/// Raised when a value supplied to the domain breaks an invariant
/// (for example a non-positive amount or an empty cardholder name).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>Creates a validation error for the given <paramref name="field"/>.</summary>
    public DomainValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    /// <summary>The name of the field that violated the invariant.</summary>
    public string Field { get; }
}
