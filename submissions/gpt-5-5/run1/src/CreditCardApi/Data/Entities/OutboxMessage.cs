namespace CreditCardApi.Data.Entities;

public sealed class OutboxMessage
{
    private const int MaxErrorLength = 2000;

    private OutboxMessage()
    {
    }

    public OutboxMessage(Guid id, string topic, string messageKey, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        Topic = topic;
        MessageKey = messageKey;
        Payload = payload;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string Topic { get; private set; } = string.Empty;

    public string MessageKey { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public DateTimeOffset? DeadLetteredAt { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }

    public void MarkDeadLettered(DateTimeOffset deadLetteredAt)
    {
        DeadLetteredAt = deadLetteredAt;
        ProcessedAt = deadLetteredAt;
    }
}
