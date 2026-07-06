namespace CreditCardApi.Tests.Integration;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_TESTCONTAINERS"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_TESTCONTAINERS=true to run PostgreSQL/Kafka integration tests.";
        }
    }
}
