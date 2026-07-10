using CreditCardApi.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CreditCardApi.Api.Infrastructure.Data;

public class CreditCardDbContext(DbContextOptions<CreditCardDbContext> options) : DbContext(options)
{
    public DbSet<CreditCard> CreditCards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardholderName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.Xmin).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.CreditCard)
                .HasForeignKey(t => t.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Merchant).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
            entity.Property(e => e.Xmin).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
            entity.HasIndex(e => e.CreditCardId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.CreditCard)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
