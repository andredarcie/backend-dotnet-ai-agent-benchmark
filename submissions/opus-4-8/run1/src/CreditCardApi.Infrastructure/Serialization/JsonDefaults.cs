using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreditCardApi.Infrastructure.Serialization;

/// <summary>Shared JSON settings so events on the wire match the API (camelCase) exactly.</summary>
public static class JsonDefaults
{
    /// <summary>camelCase, web-style options used for Kafka payloads and the outbox.</summary>
    public static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
