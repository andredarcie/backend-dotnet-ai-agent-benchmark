namespace CreditCardApi.Infrastructure.Messaging;

/// <summary>Settings for the background outbox dispatcher.</summary>
public sealed class OutboxOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Outbox";

    /// <summary>How often the dispatcher polls for unpublished events.</summary>
    public int PollIntervalSeconds { get; set; } = 5;

    /// <summary>Maximum events drained per poll.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Attempts before an event is routed to the dead-letter topic and given up on.</summary>
    public int MaxAttempts { get; set; } = 10;
}
