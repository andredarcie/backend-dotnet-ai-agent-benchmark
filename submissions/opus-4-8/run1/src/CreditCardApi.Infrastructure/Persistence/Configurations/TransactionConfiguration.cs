using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Transaction"/>.</summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasColumnType("numeric(18,2)");
        builder.Property(t => t.Merchant).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Category).HasMaxLength(80);
        builder.Property(t => t.CreatedAt);

        // Index the foreign key (speeds joins and the "transactions for a card" query) and the sort column.
        builder.HasIndex(t => t.CreditCardId);
        builder.HasIndex(t => t.CreatedAt);

        // Optimistic concurrency via PostgreSQL's xmin system column (no extra stored column).
        builder.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
