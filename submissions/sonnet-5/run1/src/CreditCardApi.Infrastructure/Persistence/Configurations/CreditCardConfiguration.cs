using CreditCardApi.Domain.Entities;
using CreditCardApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

public sealed class CreditCardConfiguration(IPanProtector panProtector) : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.ToTable("credit_cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardholderName)
            .IsRequired()
            .HasMaxLength(200);

        // Encrypted at rest (AES-256-GCM); the plaintext PAN only ever exists in memory.
        builder.Property(c => c.CardNumber)
            .IsRequired()
            .HasMaxLength(512)
            .HasConversion(plain => panProtector.Encrypt(plain), cipher => panProtector.Decrypt(cipher));

        builder.Property(c => c.Brand)
            .HasMaxLength(50);

        builder.Property(c => c.CreditLimit)
            .IsRequired()
            .HasColumnType("numeric(19,2)");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        builder.HasMany(c => c.Transactions)
            .WithOne(t => t.CreditCard!)
            .HasForeignKey(t => t.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
