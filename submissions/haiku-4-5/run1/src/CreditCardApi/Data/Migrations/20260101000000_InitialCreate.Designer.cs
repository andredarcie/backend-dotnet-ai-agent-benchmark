using System;
using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreditCardApi.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260101000000_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CreditCardApi.Domain.Entities.CreditCard", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<string>("Brand")
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<string>("CardholderName")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.Property<string>("CardNumber")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.Property<decimal>("CreditLimit")
                    .HasPrecision(18, 2)
                    .HasColumnType("numeric(18,2)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<byte[]>("RowVersion")
                    .IsConcurrencyToken()
                    .IsRequired()
                    .HasColumnType("bytea");

                b.HasKey("Id");

                b.HasIndex("CreatedAt");

                b.ToTable("CreditCards");
            });

            modelBuilder.Entity("CreditCardApi.Domain.Entities.Transaction", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<decimal>("Amount")
                    .HasPrecision(18, 2)
                    .HasColumnType("numeric(18,2)");

                b.Property<string>("Category")
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<int>("CreditCardId")
                    .HasColumnType("integer");

                b.Property<string>("Merchant")
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnType("character varying(255)");

                b.Property<byte[]>("RowVersion")
                    .IsConcurrencyToken()
                    .IsRequired()
                    .HasColumnType("bytea");

                b.HasKey("Id");

                b.HasIndex("CreditCardId");

                b.HasIndex("CreatedAt");

                b.ToTable("Transactions");
            });

            modelBuilder.Entity("CreditCardApi.Domain.Entities.Transaction", b =>
            {
                b.HasOne("CreditCardApi.Domain.Entities.CreditCard", "CreditCard")
                    .WithMany("Transactions")
                    .HasForeignKey("CreditCardId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("CreditCard");
            });

            modelBuilder.Entity("CreditCardApi.Domain.Entities.CreditCard", b =>
            {
                b.Navigation("Transactions");
            });
#pragma warning restore 612, 618
        }
    }
}
