using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CreditCardApi.Api.Observability;

/// <summary>Names and instruments for the service's own traces and metrics.</summary>
public static class DiagnosticsConfig
{
    /// <summary>Logical service name reported to OpenTelemetry.</summary>
    public const string ServiceName = "credit-card-api";

    /// <summary>Activity source for custom spans.</summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    /// <summary>Meter for custom metrics.</summary>
    public static readonly Meter Meter = new(ServiceName);

    /// <summary>Counts transactions successfully created via the API.</summary>
    public static readonly Counter<long> TransactionsCreated =
        Meter.CreateCounter<long>("credit_card_api.transactions.created", unit: "{transaction}",
            description: "Number of transactions created.");
}
