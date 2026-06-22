using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

/// <summary>
/// The only type aware of EF Core. Repositories are the sole consumers of this context.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CardholderName).IsRequired();
            entity.Property(c => c.CardNumber).IsRequired();
            entity.Property(c => c.Brand);
            entity.Property(c => c.CreditLimit).HasColumnType("numeric(18,2)");
            entity.Property(c => c.CreatedAt);

            entity.HasMany(c => c.Transactions)
                  .WithOne(t => t.CreditCard)
                  .HasForeignKey(t => t.CreditCardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasColumnType("numeric(18,2)");
            entity.Property(t => t.Merchant).IsRequired();
            entity.Property(t => t.Category);
            entity.Property(t => t.CreatedAt);

            entity.HasIndex(t => t.CreditCardId);
        });
    }
}
