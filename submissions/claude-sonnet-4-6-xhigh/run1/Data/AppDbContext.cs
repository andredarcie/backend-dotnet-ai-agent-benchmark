using CreditCardApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreditCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CardholderName).IsRequired();
            e.Property(c => c.CardNumber).IsRequired();
            e.Property(c => c.Brand).IsRequired(false);
            e.Property(c => c.CreditLimit).HasColumnType("numeric").IsRequired();
            e.Property(c => c.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("numeric").IsRequired();
            e.Property(t => t.Merchant).IsRequired();
            e.Property(t => t.Category).IsRequired(false);
            e.Property(t => t.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            e.HasOne(t => t.CreditCard)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CreditCardId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
