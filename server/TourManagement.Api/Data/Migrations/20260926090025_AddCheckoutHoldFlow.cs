using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutHoldFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckoutId",
                table: "VehicleBookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "VehicleBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckoutId",
                table: "HotelBookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "HotelBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripCheckouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    TravelerId = table.Column<int>(type: "integer", nullable: false),
                    HotelBookingId = table.Column<int>(type: "integer", nullable: true),
                    VehicleBookingId = table.Column<int>(type: "integer", nullable: true),
                    HotelPriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VehiclePriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HoldExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCheckouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripCheckouts_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TripCheckouts_Users_TravelerId",
                        column: x => x.TravelerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_CheckoutId",
                table: "VehicleBookings",
                column: "CheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_HoldExpiresAt",
                table: "VehicleBookings",
                column: "HoldExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings",
                column: "CheckoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_HoldExpiresAt",
                table: "HotelBookings",
                column: "HoldExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckouts_HoldExpiresAt",
                table: "TripCheckouts",
                column: "HoldExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckouts_Status",
                table: "TripCheckouts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckouts_TravelerId",
                table: "TripCheckouts",
                column: "TravelerId");

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckouts_TripId",
                table: "TripCheckouts",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelBookings_TripCheckouts_CheckoutId",
                table: "HotelBookings",
                column: "CheckoutId",
                principalTable: "TripCheckouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_TripCheckouts_CheckoutId",
                table: "VehicleBookings",
                column: "CheckoutId",
                principalTable: "TripCheckouts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelBookings_TripCheckouts_CheckoutId",
                table: "HotelBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleBookings_TripCheckouts_CheckoutId",
                table: "VehicleBookings");

            migrationBuilder.DropTable(
                name: "TripCheckouts");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_CheckoutId",
                table: "VehicleBookings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_HoldExpiresAt",
                table: "VehicleBookings");

            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings");

            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_HoldExpiresAt",
                table: "HotelBookings");

            migrationBuilder.DropColumn(
                name: "CheckoutId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "CheckoutId",
                table: "HotelBookings");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "HotelBookings");
        }
    }
}
