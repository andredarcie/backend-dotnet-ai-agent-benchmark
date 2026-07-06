namespace CreditCardApi.Application.Abstractions;

/// <summary>Exposes the correlation id of the request currently being processed, if any.</summary>
public interface ICorrelationIdProvider
{
    string? Current { get; }
}
