using System;

namespace CreditCardApi.Domain.Entities;

/// <summary>
/// Entity representing a message in the Transactional Outbox.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }

    public OutboxMessage() { }

    public OutboxMessage(Guid id, string type, string content)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = DateTime.UtcNow;
    }
}
