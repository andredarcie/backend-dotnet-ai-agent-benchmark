namespace CreditCardApi.Application.Exceptions;

/// <summary>
/// Thrown when a well-formed request violates a business rule — e.g. it references a credit card
/// that does not exist. Translated to an HTTP 400 problem response by the global exception handler.
/// </summary>
public sealed class BusinessRuleViolationException : Exception
{
    /// <summary>Creates the exception with a client-safe description of the violated rule.</summary>
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }
}
