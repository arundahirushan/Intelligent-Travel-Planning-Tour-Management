using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Destinations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Destinations",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Destinations");
        }
    }
}
