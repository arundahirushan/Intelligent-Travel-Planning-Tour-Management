using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupportMultipleHotelsPerCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSnapshot",
                table: "HotelBookings",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings",
                column: "CheckoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings");

            migrationBuilder.DropColumn(
                name: "PriceSnapshot",
                table: "HotelBookings");

            migrationBuilder.CreateIndex(
                name: "IX_HotelBookings_CheckoutId",
                table: "HotelBookings",
                column: "CheckoutId",
                unique: true);
        }
    }
}
