using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TourManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TripProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProposalId = table.Column<string>(type: "text", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    InputSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TravelerDecision = table.Column<string>(type: "text", nullable: true),
                    TravelerDecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminId = table.Column<int>(type: "integer", nullable: true),
                    AdminDecision = table.Column<string>(type: "text", nullable: true),
                    AdminDecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CheckoutId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripProposals_TripCheckouts_CheckoutId",
                        column: x => x.CheckoutId,
                        principalTable: "TripCheckouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TripProposals_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripProposals_Users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripProposalId = table.Column<int>(type: "integer", nullable: false),
                    AgentIdentity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: true),
                    ResultSummary = table.Column<string>(type: "jsonb", nullable: true),
                    ValidationResults = table.Column<string>(type: "jsonb", nullable: true),
                    Errors = table.Column<string>(type: "jsonb", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    FinalOutcome = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionSummaries_TripProposals_TripProposalId",
                        column: x => x.TripProposalId,
                        principalTable: "TripProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSummaries_TripProposalId",
                table: "ExecutionSummaries",
                column: "TripProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_AdminId",
                table: "TripProposals",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_CheckoutId",
                table: "TripProposals",
                column: "CheckoutId");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_ProposalId",
                table: "TripProposals",
                column: "ProposalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_RequestId",
                table: "TripProposals",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_Status",
                table: "TripProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripProposals_TripId",
                table: "TripProposals",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionSummaries");

            migrationBuilder.DropTable(
                name: "TripProposals");
        }
    }
}
