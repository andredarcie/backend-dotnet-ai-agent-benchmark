namespace CreditCardApi.Infrastructure.Messaging;

public sealed record DeadLetterEnvelope(
    string Topic,
    string? Key,
    string? Value,
    string Error,
    DateTimeOffset FailedAt);
