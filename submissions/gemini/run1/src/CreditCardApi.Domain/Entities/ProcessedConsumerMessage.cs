using System;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// Entity representing a processed message key for idempotent consumer deduplication.
/// </summary>
public class ProcessedConsumerMessage
{
    public string MessageId { get; set; } = null!;
    public DateTime ProcessedAtUtc { get; set; }

    public ProcessedConsumerMessage() { }

    public ProcessedConsumerMessage(string messageId)
    {
        MessageId = messageId;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
