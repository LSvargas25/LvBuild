using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectInventoryItems_ProjectId_MaterialId",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Workers",
                type: "int",
                nullable: true
            );

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "ProjectInventoryItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int"
            );

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "ProjectInventoryItems",
                type: "int",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginBranchId = table.Column<int>(type: "int", nullable: false),
                    DestinationBranchId = table.Column<int>(type: "int", nullable: true),
                    DestinationProjectId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    SentByUserId = table.Column<int>(type: "int", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ValidatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.CheckConstraint(
                        "CK_InventoryMovements_ExactlyOneDestination",
                        "([DestinationBranchId] IS NOT NULL AND [DestinationProjectId] IS NULL) OR ([DestinationBranchId] IS NULL AND [DestinationProjectId] IS NOT NULL)"
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Branches_DestinationBranchId",
                        column: x => x.DestinationBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Branches_OriginBranchId",
                        column: x => x.OriginBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Projects_DestinationProjectId",
                        column: x => x.DestinationProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Users_ValidatedByUserId",
                        column: x => x.ValidatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Workers_BranchId",
                table: "Workers",
                column: "BranchId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_ProductId",
                table: "ProjectInventoryItems",
                column: "ProductId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_ProjectId_MaterialId",
                table: "ProjectInventoryItems",
                columns: new[] { "ProjectId", "MaterialId" },
                unique: true,
                filter: "[MaterialId] IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_ProjectId_ProductId",
                table: "ProjectInventoryItems",
                columns: new[] { "ProjectId", "ProductId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL"
            );

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProjectInventoryItems_ExactlyOneCatalogReference",
                table: "ProjectInventoryItems",
                sql: "([MaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([MaterialId] IS NULL AND [ProductId] IS NOT NULL)"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_DestinationBranchId",
                table: "InventoryMovements",
                column: "DestinationBranchId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_DestinationProjectId",
                table: "InventoryMovements",
                column: "DestinationProjectId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OriginBranchId",
                table: "InventoryMovements",
                column: "OriginBranchId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_SentByUserId",
                table: "InventoryMovements",
                column: "SentByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ValidatedByUserId",
                table: "InventoryMovements",
                column: "ValidatedByUserId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectInventoryItems_Products_ProductId",
                table: "ProjectInventoryItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Branches_BranchId",
                table: "Workers",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectInventoryItems_Products_ProductId",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.DropForeignKey(name: "FK_Workers_Branches_BranchId", table: "Workers");

            migrationBuilder.DropTable(name: "InventoryMovements");

            migrationBuilder.DropIndex(name: "IX_Workers_BranchId", table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_ProjectInventoryItems_ProductId",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.DropIndex(
                name: "IX_ProjectInventoryItems_ProjectId_MaterialId",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.DropIndex(
                name: "IX_ProjectInventoryItems_ProjectId_ProductId",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProjectInventoryItems_ExactlyOneCatalogReference",
                table: "ProjectInventoryItems"
            );

            migrationBuilder.DropColumn(name: "BranchId", table: "Workers");

            migrationBuilder.DropColumn(name: "ProductId", table: "ProjectInventoryItems");

            migrationBuilder.AlterColumn<int>(
                name: "MaterialId",
                table: "ProjectInventoryItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInventoryItems_ProjectId_MaterialId",
                table: "ProjectInventoryItems",
                columns: new[] { "ProjectId", "MaterialId" },
                unique: true
            );
        }
    }
}
