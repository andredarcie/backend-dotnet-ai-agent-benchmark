using System.Text;
using CreditCardApi.Api.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditCardApi.UnitTests.Infrastructure;

public class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteResponse_WritesJsonWithOverallStatusAndEachCheck()
    {
        var httpContext = new DefaultHttpContext();
        var body = new MemoryStream();
        httpContext.Response.Body = body;

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["postgres"] = new(HealthStatus.Healthy, "Database reachable.", TimeSpan.Zero, null, null),
                ["kafka"] = new(HealthStatus.Unhealthy, "Kafka broker unreachable.", TimeSpan.Zero, null, null),
            },
            HealthStatus.Unhealthy,
            TimeSpan.Zero);

        await HealthCheckResponseWriter.WriteResponse(httpContext, report);

        Assert.StartsWith("application/json", httpContext.Response.ContentType, StringComparison.Ordinal);
        var json = Encoding.UTF8.GetString(body.ToArray());
        Assert.Contains("\"status\":\"Unhealthy\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"postgres\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"kafka\"", json, StringComparison.Ordinal);
    }
}
