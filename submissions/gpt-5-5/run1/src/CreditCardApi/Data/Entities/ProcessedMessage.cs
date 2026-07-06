namespace CreditCardApi.Data.Entities;

public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(string messageKey, string topic, DateTimeOffset processedAt)
    {
        MessageKey = messageKey;
        Topic = topic;
        ProcessedAt = processedAt;
    }

    public string MessageKey { get; private set; } = string.Empty;

    public string Topic { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; private set; }
}
