using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayHereModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WebsiteFee",
                table: "TripCheckouts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CheckoutId",
                table: "SupplyOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "SupplyOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripCheckoutId = table.Column<int>(type: "integer", nullable: false),
                    PayHerePaymentId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_TripCheckouts_TripCheckoutId",
                        column: x => x.TripCheckoutId,
                        principalTable: "TripCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyOrders_CheckoutId",
                table: "SupplyOrders",
                column: "CheckoutId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyOrders_HoldExpiresAt",
                table: "SupplyOrders",
                column: "HoldExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PayHerePaymentId",
                table: "PaymentAttempts",
                column: "PayHerePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_TripCheckoutId",
                table: "PaymentAttempts",
                column: "TripCheckoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyOrders_TripCheckouts_CheckoutId",
                table: "SupplyOrders",
                column: "CheckoutId",
                principalTable: "TripCheckouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyOrders_TripCheckouts_CheckoutId",
                table: "SupplyOrders");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_SupplyOrders_CheckoutId",
                table: "SupplyOrders");

            migrationBuilder.DropIndex(
                name: "IX_SupplyOrders_HoldExpiresAt",
                table: "SupplyOrders");

            migrationBuilder.DropColumn(
                name: "WebsiteFee",
                table: "TripCheckouts");

            migrationBuilder.DropColumn(
                name: "CheckoutId",
                table: "SupplyOrders");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "SupplyOrders");
        }
    }
}
