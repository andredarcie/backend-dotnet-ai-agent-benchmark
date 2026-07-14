using BackendEvaluator.Core;

namespace BackendEvaluator.Evaluators;

/// <summary>Category 6 — Messaging (🟢 deterministic).
/// Producer-only essence: Roslyn detects the Kafka client, the actual publish call and a durable
/// producer config. The live "a real event landed on the topic, keyed by id" check — the real proof, and
/// the reason no Testcontainers-based messaging test is needed — is folded in from the harness
/// kafka-check consumer on --deep runs (see Cli/Runner.IngestKafka). Consumer / Outbox / DLQ are
/// intentionally out of scope (see PROMPT.md) and are NOT scored.</summary>
public sealed class MessagingEvaluator : CategoryEvaluatorBase
{
    public override int Number => 6;
    public override string Name => "Messaging";
    public override double Weight => 0.13;
    public override AutomationLevel Automation => AutomationLevel.FullAuto;

    public override Task<CategoryResult> EvaluateAsync(EvaluationContext ctx)
    {
        var r = New();
        var p = ctx.Project;
        var f = ctx.Facts;

        bool client = p.HasPackage("Confluent.Kafka") || p.HasPackage("MassTransit")
                      || f.UsesGeneric("IProducer", "ProducerBuilder");
        r.Metrics.Add(Bool("broker-client", client, "Kafka client present (Confluent.Kafka)"));

        bool publishes = f.Invokes("ProduceAsync", "Produce");
        r.Metrics.Add(Bool("publishes", publishes, "producer publishes the transaction event (Produce/ProduceAsync)"));

        bool durable = f.HasMemberAccess("Acks.All") || f.IdentifierEquals("EnableIdempotence");
        r.Metrics.Add(Bool("durable-producer", durable, "durable producer (Acks.All / idempotence)"));

        if (ctx.Options.Deep)
            r.Notes.Add("Live check (deep, harness): a real transaction event must land on the 'transactions' topic keyed by id — folded in from the harness kafka-check consumer.");
        return Task.FromResult(r);
    }
}
