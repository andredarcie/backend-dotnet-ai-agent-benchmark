namespace BackendEvaluator.Core;

public sealed class EvaluatorOptions
{
    public required string TargetPath { get; init; }
    public bool Deep { get; init; }
    public string OutputDir { get; init; } = "";

    /// <summary>Base URL of the live system under test (set by the docker-compose harness, e.g. http://app:8080).
    /// When present, dynamic checks (health/metrics probe, k6, schemathesis) run against it.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Directory where sidecar tool containers (e.g. OWASP ZAP) drop their results to be ingested.</summary>
    public string? IngestDir { get; init; }
}
