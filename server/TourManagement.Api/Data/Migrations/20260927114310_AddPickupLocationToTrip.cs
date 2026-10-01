using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPickupLocationToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PickupLatitude",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PickupLongitude",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupNote",
                table: "Trips",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupLatitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupLongitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupNote",
                table: "Trips");
        }
    }
}
