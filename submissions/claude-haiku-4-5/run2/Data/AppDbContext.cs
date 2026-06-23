using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Merchant).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValue(DateTime.UtcNow);
            entity.HasOne(e => e.CreditCard)
                .WithMany(cc => cc.Transactions)
                .HasForeignKey(e => e.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
