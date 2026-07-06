using System.Net;

namespace CreditCardApi.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class HealthAndDocsTests
{
    private readonly HttpClient _client;

    public HealthAndDocsTests(ApiFixture fixture) => _client = fixture.Client;

    [Fact]
    public async Task Health_ReturnsTheExactContractBody()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"healthy\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Liveness_IsHealthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_ReportsDatabaseAndKafkaChecks()
    {
        var response = await _client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("postgres", body, StringComparison.Ordinal);
        Assert.Contains("kafka", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Metrics_ExposesPrometheusText()
    {
        var response = await _client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("# TYPE", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Credit Card API", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
