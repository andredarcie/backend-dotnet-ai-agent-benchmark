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

public class TransactionsIntegrationTests : IAsyncLifetime
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

    private async Task<CreditCardResponse> CreateTestCreditCard()
    {
        var request = new CreateCreditCardRequest
        {
            CardholderName = "Test User",
            CardNumber = "4532015112830366",
            Brand = "VISA",
            CreditLimit = 5000m,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/credit-cards", content);
        var responseData = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CreditCardResponse>(
            responseData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_Returns201()
    {
        var card = await CreateTestCreditCard();

        var request = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 199.99m,
            Merchant = "Amazon",
            Category = "Shopping",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var responseContent = await response.Content.ReadAsStringAsync();
        var createdTransaction = JsonSerializer.Deserialize<TransactionResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        createdTransaction.Should().NotBeNull();
        createdTransaction!.CreditCardId.Should().Be(card.Id);
        createdTransaction.Amount.Should().Be(199.99m);
        createdTransaction.Merchant.Should().Be("Amazon");
        createdTransaction.Category.Should().Be("Shopping");
    }

    [Fact]
    public async Task CreateTransaction_WithZeroAmount_Returns400()
    {
        var card = await CreateTestCreditCard();

        var request = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 0m,
            Merchant = "Test",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithNegativeAmount_Returns400()
    {
        var card = await CreateTestCreditCard();

        var request = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = -50m,
            Merchant = "Test",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidCardId_Returns400()
    {
        var request = new CreateTransactionRequest
        {
            CreditCardId = 99999,
            Amount = 100m,
            Merchant = "Test",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_WithEmptyMerchant_Returns400()
    {
        var card = await CreateTestCreditCard();

        var request = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 100m,
            Merchant = "",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client!.PostAsync("/api/transactions", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTransaction_WithValidId_Returns200()
    {
        var card = await CreateTestCreditCard();

        var createRequest = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 250m,
            Merchant = "Uber",
            Category = "Travel",
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client!.PostAsync("/api/transactions", createContent);
        var createdData = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<TransactionResponse>(
            createdData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        var getResponse = await _client.GetAsync($"/api/transactions/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        var transaction = JsonSerializer.Deserialize<TransactionResponse>(
            responseContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        transaction.Should().NotBeNull();
        transaction!.Id.Should().Be(created.Id);
        transaction.Merchant.Should().Be("Uber");
    }

    [Fact]
    public async Task GetTransaction_WithInvalidId_Returns404()
    {
        var response = await _client!.GetAsync("/api/transactions/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTransaction_WithValidId_Returns204()
    {
        var card = await CreateTestCreditCard();

        var createRequest = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 75.50m,
            Merchant = "Netflix",
        };

        var createContent = new StringContent(
            JsonSerializer.Serialize(createRequest),
            Encoding.UTF8,
            "application/json");

        var createResponse = await _client!.PostAsync("/api/transactions", createContent);
        var createdData = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<TransactionResponse>(
            createdData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        var deleteResponse = await _client.DeleteAsync($"/api/transactions/{created.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/transactions/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCreditCardTransactions_Returns200()
    {
        var card = await CreateTestCreditCard();

        var createRequest1 = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 100m,
            Merchant = "Store A",
        };

        var createRequest2 = new CreateTransactionRequest
        {
            CreditCardId = card.Id,
            Amount = 50m,
            Merchant = "Store B",
        };

        var content1 = new StringContent(
            JsonSerializer.Serialize(createRequest1),
            Encoding.UTF8,
            "application/json");

        var content2 = new StringContent(
            JsonSerializer.Serialize(createRequest2),
            Encoding.UTF8,
            "application/json");

        await _client!.PostAsync("/api/transactions", content1);
        await _client.PostAsync("/api/transactions", content2);

        var response = await _client.GetAsync($"/api/credit-cards/{card.Id}/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetCreditCardTransactions_WithInvalidCardId_Returns404()
    {
        var response = await _client!.GetAsync("/api/credit-cards/99999/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
