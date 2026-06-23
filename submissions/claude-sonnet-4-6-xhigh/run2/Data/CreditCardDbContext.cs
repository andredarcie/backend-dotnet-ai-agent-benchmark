using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public class CreditCardDbContext : DbContext
{
    public CreditCardDbContext(DbContextOptions<CreditCardDbContext> options) : base(options) { }

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CardholderName).IsRequired();
            e.Property(c => c.CardNumber).IsRequired();
            e.Property(c => c.CreditLimit).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("numeric(18,2)");
            e.Property(t => t.Merchant).IsRequired();
            e.HasOne(t => t.CreditCard)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CreditCardId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
