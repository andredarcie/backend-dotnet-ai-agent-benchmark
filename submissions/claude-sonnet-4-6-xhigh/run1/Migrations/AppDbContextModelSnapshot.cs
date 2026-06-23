using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreditCardApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CreditCardApi.Models.CreditCard", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");
                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<string>("Brand")
                    .HasColumnType("text");

                b.Property<string>("CardNumber")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("CardholderName")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<decimal>("CreditLimit")
                    .HasColumnType("numeric");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");
                b.ToTable("CreditCards");
            });

            modelBuilder.Entity("CreditCardApi.Models.Transaction", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");
                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<decimal>("Amount")
                    .HasColumnType("numeric");

                b.Property<string>("Category")
                    .HasColumnType("text");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<int>("CreditCardId")
                    .HasColumnType("integer");

                b.Property<string>("Merchant")
                    .IsRequired()
                    .HasColumnType("text");

                b.HasKey("Id");
                b.HasIndex("CreditCardId");
                b.ToTable("Transactions");
            });

            modelBuilder.Entity("CreditCardApi.Models.Transaction", b =>
            {
                b.HasOne("CreditCardApi.Models.CreditCard", "CreditCard")
                    .WithMany("Transactions")
                    .HasForeignKey("CreditCardId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("CreditCard");
            });

            modelBuilder.Entity("CreditCardApi.Models.CreditCard", b =>
            {
                b.Navigation("Transactions");
            });
#pragma warning restore 612, 618
        }
    }
}
