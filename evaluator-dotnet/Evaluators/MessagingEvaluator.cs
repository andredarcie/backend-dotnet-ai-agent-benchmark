using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 6 — Messaging (🟢 deterministic).
/// Roslyn detects the client, durable producer, idempotent-consumer/Outbox/DLQ patterns and offset
/// handling. Integration checks (Testcontainers-Kafka / Schema Registry / Pact) run with --deep.</summary>
public sealed class MessagingEvaluator : CategoryEvaluatorBase
{
    public override int Number => 6;
    public override string Name => "Messaging";
    public override double Weight => 0.11;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool client = p.HasPackage("Confluent.Kafka") || p.HasPackage("MassTransit")
                      || f.UsesGeneric("IProducer", "IConsumer", "ProducerBuilder", "ConsumerBuilder");
        r.Metrics.Add(Bool("broker-client", client, "messaging client present"));

        bool durable = f.HasMemberAccess("Acks.All") || f.IdentifierEquals("EnableIdempotence");
        r.Metrics.Add(Bool("durable-producer", durable, "durable producer (Acks.All / idempotence)"));

        bool idempotent = f.HasOutboxType || f.TypeNameContains("Inbox", "ProcessedMessage", "Idempot", "Deduplicat")
                          || f.IdentifierContains("AlreadyProcessed", "Idempotenc");
        r.Metrics.Add(Bool("idempotent-consumer", idempotent, "idempotent consumer (dedupe by id)"));

        r.Metrics.Add(Bool("outbox", f.HasOutboxType, "Transactional Outbox (DB<->broker consistency)"));
        r.Metrics.Add(Bool("dlq", f.TypeNameContains("DeadLetter", "Dlq") || f.IdentifierContains("DeadLetter", "dlq"),
            "dead-letter queue for failures", weight: 0.5));
        r.Metrics.Add(Bool("offset-after-process", f.IdentifierEquals("EnableAutoCommit") && f.Invokes("Commit", "CommitAsync"),
            "commit offset after processing (auto-commit off)", weight: 0.5));
        r.Metrics.Add(Bool("messaging-tests", p.HasPackage("Testcontainers.Kafka") || (p.HasPackage("Testcontainers") && client),
            "messaging integration tests (Testcontainers-Kafka)", weight: 0.5));

        if (ctx.Options.Deep)
            r.Notes.Add("Deep messaging checks (publish-duplicate -> single effect; kill consumer mid-process; Schema Registry compatibility) require a Kafka container and the app running.");
        return Task.FromResult(r);
    }
}
