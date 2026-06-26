using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="CreditCard"/>.</summary>
public sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardholderName).IsRequired().HasMaxLength(200);

        // Sensitive: only the ciphertext and last four digits are stored.
        builder.Property(c => c.CardNumberCiphertext).IsRequired();
        builder.Property(c => c.CardNumberLast4).IsRequired().HasMaxLength(4).IsFixedLength();

        builder.Property(c => c.Brand).HasMaxLength(40);
        builder.Property(c => c.CreditLimit).HasColumnType("numeric(18,2)");
        builder.Property(c => c.CreatedAt);

        builder.HasIndex(c => c.CreatedAt);

        // Optimistic concurrency via PostgreSQL's xmin system column (no extra stored column).
        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.CreditCard!)
            .HasForeignKey(t => t.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
