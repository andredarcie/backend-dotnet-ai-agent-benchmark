using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CreditCardApi.Tests.Integration;

public class TransactionsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("creditcard")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private CreditCardDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connectionString = _container.GetConnectionString();

        var services = new ServiceCollection();
        services.AddDbContext<CreditCardDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        _dbContext = serviceProvider.GetRequiredService<CreditCardDbContext>();
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _dbContext?.Dispose();
        await _container.StopAsync();
    }

    private async Task<CreditCard> CreateTestCard()
    {
        var card = new CreditCard
        {
            CardholderName = "Test Cardholder",
            CardNumber = "4532015112830366",
            Brand = "VISA",
            CreditLimit = 5000,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CreditCards.Add(card);
        await _dbContext.SaveChangesAsync();
        return card;
    }

    [Fact]
    public async Task CanCreateAndRetrieveTransaction()
    {
        var card = await CreateTestCard();

        var transaction = new Transaction
        {
            CreditCardId = card.Id,
            Amount = 99.99m,
            Merchant = "Amazon",
            Category = "Shopping",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(99.99m, retrieved.Amount);
        Assert.Equal("Amazon", retrieved.Merchant);
        Assert.Equal(card.Id, retrieved.CreditCardId);
    }

    [Fact]
    public async Task TransactionForeignKeyConstraintWorks()
    {
        var transaction = new Transaction
        {
            CreditCardId = 99999,
            Amount = 50.00m,
            Merchant = "Store",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);

        await Assert.ThrowsAnyAsync<Exception>(async () => await _dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CanDeleteTransaction()
    {
        var card = await CreateTestCard();

        var transaction = new Transaction
        {
            CreditCardId = card.Id,
            Amount = 50.00m,
            Merchant = "Store",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();
        var id = transaction.Id;

        _dbContext.Transactions.Remove(transaction);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == id);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CanUpdateTransaction()
    {
        var card = await CreateTestCard();

        var transaction = new Transaction
        {
            CreditCardId = card.Id,
            Amount = 100.00m,
            Merchant = "Store",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        transaction.Amount = 150.00m;
        transaction.Merchant = "Updated Store";
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(150.00m, retrieved.Amount);
        Assert.Equal("Updated Store", retrieved.Merchant);
    }

    [Fact]
    public async Task CascadeDeleteWorks()
    {
        var card = await CreateTestCard();

        var transaction1 = new Transaction
        {
            CreditCardId = card.Id,
            Amount = 100.00m,
            Merchant = "Store1",
            CreatedAt = DateTime.UtcNow
        };

        var transaction2 = new Transaction
        {
            CreditCardId = card.Id,
            Amount = 50.00m,
            Merchant = "Store2",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Transactions.AddRange(transaction1, transaction2);
        await _dbContext.SaveChangesAsync();

        _dbContext.CreditCards.Remove(card);
        await _dbContext.SaveChangesAsync();

        var transactions = await _dbContext.Transactions
            .Where(t => t.CreditCardId == card.Id)
            .ToListAsync();

        Assert.Empty(transactions);
    }
}
