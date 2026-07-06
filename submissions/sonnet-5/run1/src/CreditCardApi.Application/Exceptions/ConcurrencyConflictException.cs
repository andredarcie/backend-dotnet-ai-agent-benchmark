namespace CreditCardApi.Application.Exceptions;

/// <summary>Thrown when an update loses an optimistic-concurrency race against another writer. Maps to HTTP 409.</summary>
public sealed class ConcurrencyConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
