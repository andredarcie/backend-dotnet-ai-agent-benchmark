using System.Net;
using System.Net.Http.Json;
using CreditCardApi.Api.Application.Dto;
using CreditCardApi.Api.Domain.Entities;
using CreditCardApi.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CreditCardApi.Tests.Integration;

public class CreditCardsIntegrationTests : IAsyncLifetime
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

    [Fact]
    public async Task CanCreateAndRetrieveCreditCard()
    {
        var card = new CreditCard
        {
            CardholderName = "John Doe",
            CardNumber = "4532015112830366",
            Brand = "VISA",
            CreditLimit = 5000,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CreditCards.Add(card);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.CreditCards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == card.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("John Doe", retrieved.CardholderName);
        Assert.Equal("VISA", retrieved.Brand);
    }

    [Fact]
    public async Task CanDeleteCreditCard()
    {
        var card = new CreditCard
        {
            CardholderName = "Jane Doe",
            CardNumber = "5425233010103442",
            CreditLimit = 10000,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CreditCards.Add(card);
        await _dbContext.SaveChangesAsync();
        var id = card.Id;

        _dbContext.CreditCards.Remove(card);
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.CreditCards
            .FirstOrDefaultAsync(c => c.Id == id);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CanUpdateCreditCard()
    {
        var card = new CreditCard
        {
            CardholderName = "Test User",
            CardNumber = "4532015112830366",
            CreditLimit = 3000,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CreditCards.Add(card);
        await _dbContext.SaveChangesAsync();

        card.CreditLimit = 5000;
        await _dbContext.SaveChangesAsync();

        var retrieved = await _dbContext.CreditCards
            .FirstOrDefaultAsync(c => c.Id == card.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(5000, retrieved.CreditLimit);
    }

    [Fact]
    public async Task PaginationWorks()
    {
        for (int i = 0; i < 15; i++)
        {
            _dbContext.CreditCards.Add(new CreditCard
            {
                CardholderName = $"Card {i}",
                CardNumber = "4532015112830366",
                CreditLimit = 1000,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        var page1 = await _dbContext.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip(0)
            .Take(10)
            .ToListAsync();

        var page2 = await _dbContext.CreditCards
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip(10)
            .Take(10)
            .ToListAsync();

        Assert.Equal(10, page1.Count);
        Assert.Equal(5, page2.Count);
    }
}
