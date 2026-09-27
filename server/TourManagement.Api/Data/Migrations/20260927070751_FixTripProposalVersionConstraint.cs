using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTripProposalVersionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripProposals_TripId",
                table: "TripProposals");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_TripId_Version",
                table: "TripProposals",
                columns: new[] { "TripId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripProposals_TripId_Version",
                table: "TripProposals");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_TripId",
                table: "TripProposals",
                column: "TripId");
        }
    }
}
