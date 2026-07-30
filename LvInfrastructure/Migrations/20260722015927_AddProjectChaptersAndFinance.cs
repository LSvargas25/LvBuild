using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectChaptersAndFinance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "SiteLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "Payrolls",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "MaterialTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "Incidents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectChapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    AssignedSoldTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualCostTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChapterProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncidentCount = table.Column<int>(type: "int", nullable: false),
                    IncidentPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectChapters_BudgetChapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "BudgetChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectChapters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogs_ChapterId",
                table: "SiteLogs",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ChapterId",
                table: "Payrolls",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTickets_ChapterId",
                table: "MaterialTickets",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ChapterId",
                table: "Incidents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChapters_ChapterId",
                table: "ProjectChapters",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChapters_ProjectId_ChapterId",
                table: "ProjectChapters",
                columns: new[] { "ProjectId", "ChapterId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_BudgetChapters_ChapterId",
                table: "Incidents",
                column: "ChapterId",
                principalTable: "BudgetChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialTickets_BudgetChapters_ChapterId",
                table: "MaterialTickets",
                column: "ChapterId",
                principalTable: "BudgetChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_BudgetChapters_ChapterId",
                table: "Payrolls",
                column: "ChapterId",
                principalTable: "BudgetChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteLogs_BudgetChapters_ChapterId",
                table: "SiteLogs",
                column: "ChapterId",
                principalTable: "BudgetChapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_BudgetChapters_ChapterId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialTickets_BudgetChapters_ChapterId",
                table: "MaterialTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_BudgetChapters_ChapterId",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteLogs_BudgetChapters_ChapterId",
                table: "SiteLogs");

            migrationBuilder.DropTable(
                name: "ProjectChapters");

            migrationBuilder.DropIndex(
                name: "IX_SiteLogs_ChapterId",
                table: "SiteLogs");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_ChapterId",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_MaterialTickets_ChapterId",
                table: "MaterialTickets");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ChapterId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "SiteLogs");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "MaterialTickets");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "Incidents");
        }
    }
}
    