using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LvInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteLogModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteLogs",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PendingTasks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalPayroll = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalMaterials = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProgressPercentage = table.Column<decimal>(
                        type: "decimal(5,2)",
                        nullable: true
                    ),
                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SiteLogs_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SiteLogs_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SiteLogEquipment",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteLogId = table.Column<int>(type: "int", nullable: false),
                    EquipmentType = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteLogEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteLogEquipment_SiteLogs_SiteLogId",
                        column: x => x.SiteLogId,
                        principalTable: "SiteLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SiteLogMaterials",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteLogId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    QuantityUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteLogMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteLogMaterials_MaterialCatalogs_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "MaterialCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_SiteLogMaterials_SiteLogs_SiteLogId",
                        column: x => x.SiteLogId,
                        principalTable: "SiteLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SiteLogWorkers",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteLogId = table.Column<int>(type: "int", nullable: false),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteLogWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteLogWorkers_SiteLogs_SiteLogId",
                        column: x => x.SiteLogId,
                        principalTable: "SiteLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_SiteLogWorkers_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogEquipment_SiteLogId",
                table: "SiteLogEquipment",
                column: "SiteLogId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogMaterials_MaterialId",
                table: "SiteLogMaterials",
                column: "MaterialId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogMaterials_SiteLogId",
                table: "SiteLogMaterials",
                column: "SiteLogId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogs_ApprovedByUserId",
                table: "SiteLogs",
                column: "ApprovedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogs_CreatedByUserId",
                table: "SiteLogs",
                column: "CreatedByUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogs_ProjectId_WeekStart",
                table: "SiteLogs",
                columns: new[] { "ProjectId", "WeekStart" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogWorkers_SiteLogId",
                table: "SiteLogWorkers",
                column: "SiteLogId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SiteLogWorkers_WorkerId",
                table: "SiteLogWorkers",
                column: "WorkerId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SiteLogEquipment");

            migrationBuilder.DropTable(name: "SiteLogMaterials");

            migrationBuilder.DropTable(name: "SiteLogWorkers");

            migrationBuilder.DropTable(name: "SiteLogs");
        }
    }
}
