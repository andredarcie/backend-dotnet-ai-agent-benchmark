using Microsoft.EntityFrameworkCore;
using CreditCardApi.Domain;

namespace CreditCardApi.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<CreditCard> CreditCards { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CreditCard>(entity =>
            {
                entity.ToTable("CreditCards");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CardholderName).IsRequired();
                entity.Property(e => e.CardNumber).IsRequired();
                entity.Property(e => e.CreditLimit).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Merchant).IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

                entity.HasOne(d => d.CreditCard)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(d => d.CreditCardId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
