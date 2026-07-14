namespace CreditCardApi.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. The global exception handler maps this to 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
