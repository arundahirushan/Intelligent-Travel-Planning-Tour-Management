using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveContractPerSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_SupplierId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractRequests_SupplierId",
                table: "ContractRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_SupplierId_Active_Unique",
                table: "Contracts",
                column: "SupplierId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRequests_SupplierId_Pending_Unique",
                table: "ContractRequests",
                column: "SupplierId",
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Contracts_SupplierId_Active_Unique",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_ContractRequests_SupplierId_Pending_Unique",
                table: "ContractRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_SupplierId",
                table: "Contracts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractRequests_SupplierId",
                table: "ContractRequests",
                column: "SupplierId");
        }
    }
}
