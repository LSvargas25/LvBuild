using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaterialsUsedCount",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "MaterialTickets",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    InvoicePhotoPath = table.Column<string>(
                        type: "nvarchar(300)",
                        maxLength: 300,
                        nullable: true
                    ),
                    MaterialName = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialTickets_MaterialCatalogs_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MaterialCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_MaterialTickets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_MaterialTickets_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_MaterialTickets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ProjectInventoryItems",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceUnitCost = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInventoryItems_MaterialCatalogs_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MaterialCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_ProjectInventoryItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTickets_CreatedByUserId",
                table: "MaterialTickets",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTickets_MaterialId",
                table: "MaterialTickets",
                column: "MaterialId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTickets_ProjectId",
                table: "MaterialTickets",
                column: "ProjectId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTickets_SupplierId",
                table: "MaterialTickets",
                column: "SupplierId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_MaterialId",
                table: "ProjectInventoryItems",
                column: "MaterialId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_ProjectId_MaterialId",
                table: "ProjectInventoryItems",
                columns: new[] { "ProjectId", "MaterialId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MaterialTickets");

            migrationBuilder.DropTable(name: "ProjectInventoryItems");

            migrationBuilder.DropColumn(name: "MaterialsUsedCount", table: "Projects");
        }
    }
}
