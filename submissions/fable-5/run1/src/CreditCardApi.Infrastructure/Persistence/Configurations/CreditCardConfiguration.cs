using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

internal sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.Property(c => c.CardholderName)
            .HasMaxLength(200)
            .IsRequired();

        // Only the truncated PAN is ever stored; see Domain.Cards.CardNumber.
        builder.Property(c => c.CardNumberLast4)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(c => c.Brand)
            .HasMaxLength(50);

        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 2);

        // PostgreSQL's xmin system column doubles as an optimistic-concurrency token
        // (a uint rowversion property maps to xmin by provider convention).
        builder.Property<uint>("xmin").IsRowVersion();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_CreditCards_CreditLimit_NonNegative",
            "\"CreditLimit\" >= 0"));
    }
}
