namespace CreditCardApi.Domain.Common;

public interface IConcurrencyTracked
{
    int Version { get; }

    void IncrementVersion();
}
