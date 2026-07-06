namespace CreditCardApi.Application.Exceptions;

public sealed class InvalidRequestException : Exception
{
    public InvalidRequestException(IReadOnlyDictionary<string, string[]> errors)
        : base("The request is invalid.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
