using System.Text.Json;

namespace CreditCardApi.IntegrationTests;

internal static class Contracts
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
