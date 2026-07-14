namespace BackendEvaluator.Core;

public sealed class EvaluatorOptions
{
    public required string TargetPath { get; init; }
    public bool Deep { get; init; }
    public string OutputDir { get; init; } = "";

    /// <summary>Base URL of the live system under test (set by the docker-compose harness, e.g. http://app:8080).
    /// When present, the live checks (health/metrics probes, the OpenAPI probe, the contract oracle) run against it.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Directory where the harness's sidecar (kafka-check) drops what it observed, to be ingested.</summary>
    public string? IngestDir { get; init; }
}
