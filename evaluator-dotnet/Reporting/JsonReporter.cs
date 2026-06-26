using System.Text.Json;
using System.Text.Json.Serialization;
using BackendEvaluator.Core;

namespace BackendEvaluator.Reporting;

public static class JsonReporter
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Write(EvaluationReport report, string path)
    {
        var json = JsonSerializer.Serialize(report, Opts);
        File.WriteAllText(path, json);
        return path;
    }
}
