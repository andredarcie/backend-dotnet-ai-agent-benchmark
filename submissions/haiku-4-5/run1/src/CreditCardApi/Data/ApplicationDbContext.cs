using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCardApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CreditCard> CreditCards { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<OutboxEvent> OutboxEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreditCard>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CardholderName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Brand).HasMaxLength(50);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Merchant).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => e.CreditCardId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.CreditCard)
                .WithMany(cc => cc.Transactions)
                .HasForeignKey(e => e.CreditCardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.ProcessedAt).HasColumnType("timestamp with time zone");
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
