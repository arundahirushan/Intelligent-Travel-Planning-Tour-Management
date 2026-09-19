using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBookingsToHotelBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the table — preserves all existing rows.
            migrationBuilder.RenameTable(
                name: "Bookings",
                newName: "HotelBookings");

            // Rename the indexes and primary key constraint to match the new table name.
            migrationBuilder.RenameIndex(
                name: "IX_Bookings_TripId",
                table: "HotelBookings",
                newName: "IX_HotelBookings_TripId");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_Status",
                table: "HotelBookings",
                newName: "IX_HotelBookings_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Bookings_RoomId",
                table: "HotelBookings",
                newName: "IX_HotelBookings_RoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: rename HotelBookings back to Bookings.
            migrationBuilder.RenameIndex(
                name: "IX_HotelBookings_TripId",
                table: "Bookings",
                newName: "IX_Bookings_TripId");

            migrationBuilder.RenameIndex(
                name: "IX_HotelBookings_Status",
                table: "Bookings",
                newName: "IX_Bookings_Status");

            migrationBuilder.RenameIndex(
                name: "IX_HotelBookings_RoomId",
                table: "Bookings",
                newName: "IX_Bookings_RoomId");

            migrationBuilder.RenameTable(
                name: "HotelBookings",
                newName: "Bookings");
        }
    }
}
