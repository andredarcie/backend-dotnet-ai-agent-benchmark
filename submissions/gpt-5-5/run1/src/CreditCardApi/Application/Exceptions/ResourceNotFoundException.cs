namespace CreditCardApi.Application.Exceptions;

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resourceName, object id)
        : base($"{resourceName} '{id}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = id;
    }

    public string ResourceName { get; }

    public object ResourceId { get; }
}
