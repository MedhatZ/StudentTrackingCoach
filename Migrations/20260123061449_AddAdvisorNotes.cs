using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentTrackingCoach.Migrations
{
    public partial class AddAdvisorNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvisorNotes",
                schema: "dbo",
                columns: table => new
                {
                    AdvisorNoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    StudentId = table.Column<long>(type: "bigint", nullable: false),

                    AdvisorUserId = table.Column<string>(
                        type: "nvarchar(450)",
                        nullable: false),

                    ActionTaken = table.Column<string>(
                        type: "nvarchar(200)",
                        nullable: false),

                    Notes = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false,
                        defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        name: "PK_AdvisorNotes",
                        columns: x => x.AdvisorNoteId
                    );
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorNotes",
                schema: "dbo");
        }
    }
}
