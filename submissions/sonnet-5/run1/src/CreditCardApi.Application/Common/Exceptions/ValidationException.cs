namespace CreditCardApi.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request fails a business rule (required field, FK existence, numeric range).
/// The global exception handler maps this to a 400 RFC 9457 Problem Details response.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
