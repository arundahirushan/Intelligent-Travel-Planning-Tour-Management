using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalIdToCheckouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProposalId",
                table: "TripCheckouts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripCheckouts_ProposalId",
                table: "TripCheckouts",
                column: "ProposalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripCheckouts_ProposalId",
                table: "TripCheckouts");

            migrationBuilder.DropColumn(
                name: "ProposalId",
                table: "TripCheckouts");
        }
    }
}
