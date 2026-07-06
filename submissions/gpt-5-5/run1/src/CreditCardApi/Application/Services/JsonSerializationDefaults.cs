using System.Text.Json;

namespace CreditCardApi.Application.Services;

public static class JsonSerializationDefaults
{
    public static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);
}
