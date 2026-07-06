using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardholderName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CardNumberLast4).HasMaxLength(4).IsRequired();
        builder.Property(c => c.Brand).HasMaxLength(30);
        builder.Property(c => c.CreditLimit).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz").IsRequired();

        // Optimistic concurrency via Postgres' system column — no client-visible version field needed.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.Navigation(c => c.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.CreditCard)
            .HasForeignKey(t => t.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
