namespace CreditCardApi.IntegrationTests;

/// <summary>Shares one set of (expensive) Postgres + Kafka containers across every test class.</summary>
[CollectionDefinition(Name)]
public class ApiCollectionDefinition : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
