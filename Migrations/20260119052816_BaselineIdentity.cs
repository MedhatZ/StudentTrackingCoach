using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentTrackingCoach.Migrations
{
    /// <inheritdoc />
    public partial class BaselineIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BASELINE MIGRATION
            // Existing schema (Identity + domain tables) already present.
            // No changes applied.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback for baseline migration
        }
    }
}
