namespace CreditCardApi.Domain.Entities;

public class OutboxEvent
{
    public int Id { get; set; }

    public required string Topic { get; set; }

    public required string Key { get; set; }

    public required string Payload { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
