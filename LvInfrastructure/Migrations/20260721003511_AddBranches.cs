using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: false
                    ),
                    PhoneNumber = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: true
                    ),
                    Email = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: true
                    ),
                    City = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Province = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    BranchType = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    OperationsDirectorId = table.Column<int>(type: "int", nullable: false),
                    BranchAdminId = table.Column<int>(type: "int", nullable: true),
                    BusinessManagerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Users_BranchAdminId",
                        column: x => x.BranchAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Branches_Users_BusinessManagerId",
                        column: x => x.BusinessManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Branches_Users_OperationsDirectorId",
                        column: x => x.OperationsDirectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "BranchIndicators",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Profit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Losses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DirectExpenses = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IndirectExpenses = table.Column<decimal>(
                        type: "decimal(18,2)",
                        nullable: false
                    ),
                    TotalWorkers = table.Column<int>(type: "int", nullable: false),
                    TotalMaterials = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchIndicators_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchAdminId",
                table: "Branches",
                column: "BranchAdminId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BusinessManagerId",
                table: "Branches",
                column: "BusinessManagerId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Branches_OperationsDirectorId",
                table: "Branches",
                column: "OperationsDirectorId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_BranchIndicators_BranchId",
                table: "BranchIndicators",
                column: "BranchId",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BranchIndicators");

            migrationBuilder.DropTable(name: "Branches");
        }
    }
}
