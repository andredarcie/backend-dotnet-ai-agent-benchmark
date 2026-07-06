using System;
using CreditCardApi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CreditCardApi.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260706000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "credit_cards",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                cardholder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                card_number_cipher_text = table.Column<string>(type: "text", nullable: false),
                card_number_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_credit_cards", x => x.id);
                table.CheckConstraint("ck_credit_cards_credit_limit_non_negative", "credit_limit >= 0");
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                message_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                attempts = table.Column<int>(type: "integer", nullable: false),
                last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "processed_messages",
            columns: table => new
            {
                message_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_processed_messages", x => x.message_key);
            });

        migrationBuilder.CreateTable(
            name: "transactions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                credit_card_id = table.Column<int>(type: "integer", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                merchant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_transactions", x => x.id);
                table.CheckConstraint("ck_transactions_amount_positive", "amount > 0");
                table.ForeignKey(
                    name: "fk_transactions_credit_cards_credit_card_id",
                    column: x => x.credit_card_id,
                    principalTable: "credit_cards",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "ix_credit_cards_created_at", table: "credit_cards", column: "created_at");
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_message_key", table: "outbox_messages", column: "message_key");
        migrationBuilder.CreateIndex(name: "ix_outbox_messages_unprocessed", table: "outbox_messages", columns: ["processed_at", "occurred_at"]);
        migrationBuilder.CreateIndex(name: "ix_processed_messages_processed_at", table: "processed_messages", column: "processed_at");
        migrationBuilder.CreateIndex(name: "ix_transactions_created_at", table: "transactions", column: "created_at");
        migrationBuilder.CreateIndex(name: "ix_transactions_credit_card_id", table: "transactions", column: "credit_card_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "outbox_messages");
        migrationBuilder.DropTable(name: "processed_messages");
        migrationBuilder.DropTable(name: "transactions");
        migrationBuilder.DropTable(name: "credit_cards");
    }
}

