using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public class CreditCardDbContext : DbContext
{
    public CreditCardDbContext(DbContextOptions<CreditCardDbContext> options) : base(options)
    {
    }

    public DbSet<CreditCard> CreditCards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardholderName).IsRequired();
            entity.Property(e => e.CardNumber).IsRequired();
            entity.Property(e => e.CreditLimit).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.CreditCard)
                .HasForeignKey(t => t.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreditCardId).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Merchant).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasOne(e => e.CreditCard)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
