namespace CreditCardApi.Tests.Integration;

using CreditCardApi.Api;
using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Infrastructure.Data;
using CreditCardApi.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;
using FluentAssertions;
using Moq;

public class CreditCardsIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("creditcard_db")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<CreditCardDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<CreditCardDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                    });

                    var producerDescriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(ITransactionProducer));

                    if (producerDescriptor != null)
                    {
                        services.Remove(producerDescriptor);
                    }

                    // Remove and replace Kafka-related services with mocks for testing
                    var kafkaConfigDescriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(Microsoft.Extensions.Options.IOptions<CreditCardApi.Api.Infrastructure.Messaging.KafkaProducerConfig>));
                    if (kafkaConfigDescriptor != null)
                    {
                        services.Remove(kafkaConfigDescriptor);
                    }

                    var mockProducer = new Mock<ITransactionProducer>();
                    mockProducer
                        .Setup(x => x.PublishTransactionCreatedAsync(It.IsAny<TransactionResponse>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
                    services.AddSingleton(mockProducer.Object);
                });
            });

        _client = _factory.CreateClient();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CreditCardDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            _client.Dispose();
        }

        if (_factory != null)
        {
            _factory.Dispose();
        }

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateCreditCard_WithValidData_Returns201()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "John Doe",
            CardNumber = "4532015112830366",
            Brand = "VISA",
            CreditLimit = 5000m,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/credit-cards", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdCard = JsonSerializer.Deserialize<CreditCardResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        createdCard.Should().NotBeNull();
        createdCard!.CardholderName.Should().Be("John Doe");
        createdCard.CardNumber.Should().Be("4532015112830366");
        createdCard.Brand.Should().Be("VISA");
        createdCard.CreditLimit.Should().Be(5000m);
    }

    [Fact]
    public async Task CreateCreditCard_WithEmptyCardholderName_Returns400()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "",
            CardNumber = "4532015112830366",
            CreditLimit = 5000m,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/credit-cards", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCreditCard_WithValidId_Returns200()
    {
        var createRequest = new CreateCreditCardRequest
        {
            CardholderName = "Jane Doe",
            CardNumber = "5425233010103442",
            Brand = "MASTERCARD",
            CreditLimit = 10000m,
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client!.PostAsync("/api/credit-cards", createContent);
        var createdData = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CreditCardResponse>(
            createdData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        var getResponse = await _client.GetAsync($"/api/credit-cards/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        var card = JsonSerializer.Deserialize<CreditCardResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        card.Should().NotBeNull();
        card!.Id.Should().Be(created.Id);
        card.CardholderName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task GetCreditCard_WithInvalidId_Returns404()
    {
        var response = await _client!.GetAsync("/api/credit-cards/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCreditCard_WithValidData_Returns204()
    {
        var createRequest = new CreateCreditCardRequest
        {
            CardholderName = "Update Test",
            CardNumber = "6011000990139424",
            CreditLimit = 3000m,
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client!.PostAsync("/api/credit-cards", createContent);
        var createdData = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CreditCardResponse>(
            createdData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        var updateRequest = new UpdateCreditCardRequest
        {
            CreditLimit = 5000m,
        };

        var updateContent = new StringContent(
            JsonSerializer.Serialize(updateRequest),
            Encoding.UTF8,
            "application/json");

        var updateResponse = await _client.PutAsync($"/api/credit-cards/{created.Id}", updateContent);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCreditCard_WithValidId_Returns204()
    {
        var createRequest = new CreateCreditCardRequest
        {
            CardholderName = "Delete Test",
            CardNumber = "3782822463100005",
            CreditLimit = 2000m,
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client!.PostAsync("/api/credit-cards", createContent);
        var createdData = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<CreditCardResponse>(
            createdData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        var deleteResponse = await _client.DeleteAsync($"/api/credit-cards/{created.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/credit-cards/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCreditCards_Returns200WithPaginatedData()
    {
        var createRequest1 = new CreateCreditCardRequest
        {
            CardholderName = "Paginated Test 1",
            CardNumber = "4024007134432500",
            CreditLimit = 1000m,
        };

        var createRequest2 = new CreateCreditCardRequest
        {
            CardholderName = "Paginated Test 2",
            CardNumber = "5105105105105100",
            CreditLimit = 2000m,
        };

        var content1 = new StringContent(
            JsonSerializer.Serialize(createRequest1),
            Encoding.UTF8,
            "application/json");

        var content2 = new StringContent(
            JsonSerializer.Serialize(createRequest2),
            Encoding.UTF8,
            "application/json");

        await _client!.PostAsync("/api/credit-cards", content1);
        await _client.PostAsync("/api/credit-cards", content2);

        var response = await _client.GetAsync("/api/credit-cards?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);
        doc.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        doc.RootElement.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }
}
