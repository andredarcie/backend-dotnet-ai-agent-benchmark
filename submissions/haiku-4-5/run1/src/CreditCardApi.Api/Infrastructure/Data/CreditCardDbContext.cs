namespace CreditCardApi.Api.Infrastructure.Data;

using CreditCardApi.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class CreditCardDbContext(DbContextOptions<CreditCardDbContext> options) : DbContext(options)
{
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CardholderName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(19);
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.CardNumber).IsUnique();
            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.CreditCard)
                .HasForeignKey(t => t.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CreditCardId).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Merchant).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.CreditCardId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
