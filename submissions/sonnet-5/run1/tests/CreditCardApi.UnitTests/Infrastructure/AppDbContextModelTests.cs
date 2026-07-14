using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Persistence;
using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CreditCardApi.UnitTests.Infrastructure;

/// <summary>
/// EF Core builds its model metadata the moment <see cref="DbContext.Model"/> is touched, entirely
/// in-memory - no connection is ever opened. That makes the mapping itself (converters, FKs,
/// indexes) a plain, fast, offline unit test surface.
/// </summary>
public class AppDbContextModelTests
{
    private static AppDbContext CreateSut()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        var panProtector = new AesPanProtector(Options.Create(new SecurityOptions { PanEncryptionKey = "uO+9Anw4AgetgEyDt//K7i+gwsVA0Af29+vx/QAt+/A=" })); // gitleaks:allow - test fixture

        return new AppDbContext(options, panProtector);
    }

    [Fact]
    public void Model_MapsCreditCardCardNumberThroughAValueConverter()
    {
        using var dbContext = CreateSut();

        var cardNumber = dbContext.Model.FindEntityType(typeof(CreditCard))!.FindProperty(nameof(CreditCard.CardNumber))!;

        Assert.NotNull(cardNumber.GetValueConverter());
    }

    [Fact]
    public void Model_MapsCreditCardAndTransactionToSnakeCaseTableNames()
    {
        using var dbContext = CreateSut();

        Assert.Equal("credit_cards", dbContext.Model.FindEntityType(typeof(CreditCard))!.GetTableName());
        Assert.Equal("transactions", dbContext.Model.FindEntityType(typeof(Transaction))!.GetTableName());
    }

    [Fact]
    public void Model_RestrictsDeletingACreditCardThatHasTransactions()
    {
        using var dbContext = CreateSut();

        var foreignKey = Assert.Single(dbContext.Model.FindEntityType(typeof(Transaction))!.GetForeignKeys());

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_IndexesTheTransactionForeignKeyAndCreatedAtColumns()
    {
        using var dbContext = CreateSut();

        var indexedProperties = dbContext.Model.FindEntityType(typeof(Transaction))!
            .GetIndexes()
            .SelectMany(index => index.Properties.Select(p => p.Name))
            .ToHashSet();

        Assert.Contains(nameof(Transaction.CreditCardId), indexedProperties);
        Assert.Contains(nameof(Transaction.CreatedAt), indexedProperties);
    }

    [Fact]
    public void Model_StoresMoneyColumnsAsNumericWithTwoDecimalPlaces()
    {
        using var dbContext = CreateSut();

        var creditLimit = dbContext.Model.FindEntityType(typeof(CreditCard))!.FindProperty(nameof(CreditCard.CreditLimit))!;
        var amount = dbContext.Model.FindEntityType(typeof(Transaction))!.FindProperty(nameof(Transaction.Amount))!;

        Assert.Equal("numeric(19,2)", creditLimit.GetColumnType());
        Assert.Equal("numeric(19,2)", amount.GetColumnType());
    }
}
