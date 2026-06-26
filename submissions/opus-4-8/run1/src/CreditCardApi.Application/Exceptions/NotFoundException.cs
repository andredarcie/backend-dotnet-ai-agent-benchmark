namespace CreditCardApi.Application.Exceptions;

/// <summary>Raised when a requested resource does not exist. Mapped to <c>404 Not Found</c>.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates a not-found error for the given resource and key.</summary>
    public NotFoundException(string resource, object key)
        : base($"{resource} with id '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
    }

    /// <summary>The resource type that was not found (for example "CreditCard").</summary>
    public string Resource { get; }

    /// <summary>The key that was looked up.</summary>
    public object Key { get; }
}
