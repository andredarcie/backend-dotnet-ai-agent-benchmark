namespace CreditCardApi.Application.Abstractions;

/// <summary>Supplies the current UTC time. Abstracted so use cases stay deterministic under test.</summary>
public interface IClock
{
    /// <summary>The current instant in UTC.</summary>
    DateTime UtcNow { get; }
}
