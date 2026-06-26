namespace CreditCardApi.Domain.Exceptions;

/// <summary>
/// Base type for errors that represent a violation of a domain rule or invariant.
/// These are mapped to <c>400 Bad Request</c> Problem Details at the API boundary.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates a new <see cref="DomainException"/> with the given message.</summary>
    protected DomainException(string message) : base(message)
    {
    }
}
