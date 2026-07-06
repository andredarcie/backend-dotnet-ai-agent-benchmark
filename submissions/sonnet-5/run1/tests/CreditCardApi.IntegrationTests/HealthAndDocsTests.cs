using System.Net;
using System.Text.Json;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class HealthAndDocsTests(ApiFixture fixture)
{
    [Fact]
    public async Task Health_ReturnsTheExactLivenessContract()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"healthy\"}", body);
    }

    [Fact]
    public async Task HealthReady_ReportsPostgresAndKafkaAsHealthy()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", document.RootElement.GetProperty("status").GetString());
        var checkNames = document.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("postgres", checkNames);
        Assert.Contains("kafka", checkNames);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesEveryMandatedEndpoint()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/credit-cards", paths);
        Assert.Contains("/api/credit-cards/{id}", paths);
        Assert.Contains("/api/credit-cards/{id}/transactions", paths);
        Assert.Contains("/api/transactions", paths);
        Assert.Contains("/api/transactions/{id}", paths);
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_ExposesPrometheusFormat()
    {
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("# TYPE", body);
    }
}
