using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OfferNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OfferType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityDays = table.Column<int>(type: "int", nullable: false),
                    WorkLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    WorkScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstimatedDurationWeeks = table.Column<int>(type: "int", nullable: false),
                    EstimatedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Warranties = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Exclusions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TotalProjectPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AgreedPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PercentageIncludes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PercentageExcludes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PercentageCalculationMethod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentFrequency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GeneratedPdfPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferChapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferId = table.Column<int>(type: "int", nullable: false),
                    ChapterName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EstimatedWeeks = table.Column<int>(type: "int", nullable: false),
                    ApproxMaterialQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferChapters_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfferChapters_OfferId",
                table: "OfferChapters",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_BudgetId",
                table: "Offers",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedByUserId",
                table: "Offers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CustomerId",
                table: "Offers",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OfferNumber",
                table: "Offers",
                column: "OfferNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferChapters");

            migrationBuilder.DropTable(
                name: "Offers");
        }
    }
}
