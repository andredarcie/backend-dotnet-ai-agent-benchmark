using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.ToTable(table =>
                table.HasCheckConstraint("CK_CreditCards_CreditLimit", "\"CreditLimit\" >= 0"));
            entity.HasKey(card => card.Id);
            entity.Property(card => card.Id).ValueGeneratedOnAdd();
            entity.Property(card => card.CardholderName).IsRequired();
            entity.Property(card => card.CardNumber).IsRequired();
            entity.Property(card => card.CreditLimit).HasPrecision(18, 2).IsRequired();
            entity.Property(card => card.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable(table =>
                table.HasCheckConstraint("CK_Transactions_Amount", "\"Amount\" > 0"));
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Id).ValueGeneratedOnAdd();
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(transaction => transaction.Merchant).IsRequired();
            entity.Property(transaction => transaction.CreatedAt).IsRequired();
            entity.HasOne(transaction => transaction.CreditCard)
                .WithMany(card => card.Transactions)
                .HasForeignKey(transaction => transaction.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
