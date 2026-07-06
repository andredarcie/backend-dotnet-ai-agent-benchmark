using CreditCardApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CreditCardApi.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Category)
            .HasMaxLength(100);

        builder.HasOne(t => t.CreditCard)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CreditCardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Supports the "transactions of a card" queries and the FK constraint.
        builder.HasIndex(t => t.CreditCardId);

        // Optimistic concurrency via PostgreSQL's xmin system column, as on CreditCard.
        builder.Property<uint>("xmin").IsRowVersion();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Transactions_Amount_Positive",
            "\"Amount\" > 0"));
    }
}
