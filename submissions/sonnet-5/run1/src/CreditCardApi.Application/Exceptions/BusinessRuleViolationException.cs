namespace CreditCardApi.Application.Exceptions;

/// <summary>Thrown when a request is well-formed but violates a domain business rule. Maps to HTTP 400.</summary>
public sealed class BusinessRuleViolationException(string message) : Exception(message);
