using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentTrackingCoach.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                schema: "dbo",
                table: "Student",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "Interventions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                schema: "dbo",
                table: "Interventions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AdvisorNotes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AdminAuditLogs', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE [dbo].[AdminAuditLogs] ADD [TenantId] int NOT NULL CONSTRAINT [DF_AdminAuditLogs_TenantId] DEFAULT 1;
    END
END");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PassingGrade = table.Column<int>(type: "int", nullable: false),
                    HighRiskThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                });

            migrationBuilder.CreateTable(
                name: "TenantFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FeatureKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FeatureValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantFeatures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantUserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUserRoles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Student_TenantId_StudentId",
                schema: "dbo",
                table: "Student",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_TenantId_StudentId_Status",
                schema: "dbo",
                table: "Interventions",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorNotes_TenantId_StudentId_CreatedAt",
                table: "AdvisorNotes",
                columns: new[] { "TenantId", "StudentId", "CreatedAt" });

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_AdminAuditLogs_TenantId_CreatedAt'
          AND object_id = OBJECT_ID(N'[dbo].[AdminAuditLogs]')
    )
    BEGIN
        CREATE INDEX [IX_AdminAuditLogs_TenantId_CreatedAt] ON [dbo].[AdminAuditLogs] ([TenantId], [CreatedAt]);
    END
END");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatures_TenantId_FeatureKey",
                table: "TenantFeatures",
                columns: new[] { "TenantId", "FeatureKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_TenantId_UserId_RoleName",
                table: "TenantUserRoles",
                columns: new[] { "TenantId", "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserRoles_UserId",
                table: "TenantUserRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantFeatures");

            migrationBuilder.DropTable(
                name: "TenantUserRoles");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Student_TenantId_StudentId",
                schema: "dbo",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Interventions_TenantId_StudentId_Status",
                schema: "dbo",
                table: "Interventions");

            migrationBuilder.DropIndex(
                name: "IX_AdvisorNotes_TenantId_StudentId_CreatedAt",
                table: "AdvisorNotes");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_AdminAuditLogs_TenantId_CreatedAt'
          AND object_id = OBJECT_ID(N'[dbo].[AdminAuditLogs]')
    )
    BEGIN
        DROP INDEX [IX_AdminAuditLogs_TenantId_CreatedAt] ON [dbo].[AdminAuditLogs];
    END
END");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "dbo",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "dbo",
                table: "Interventions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AdvisorNotes");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[AdminAuditLogs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AdminAuditLogs', 'TenantId') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[AdminAuditLogs] DROP CONSTRAINT IF EXISTS [DF_AdminAuditLogs_TenantId];
        ALTER TABLE [dbo].[AdminAuditLogs] DROP COLUMN [TenantId];
    END
END");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "Interventions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
