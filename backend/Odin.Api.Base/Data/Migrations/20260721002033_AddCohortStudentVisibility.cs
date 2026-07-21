using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCohortStudentVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisibleToStudents",
                table: "CohortUploadFields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Existing installs: the seeded outline field becomes student-visible.
            migrationBuilder.Sql("""
                UPDATE "CohortUploadFields" SET "VisibleToStudents" = TRUE
                WHERE "Label" = 'Module Outline Given to Students';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleToStudents",
                table: "CohortUploadFields");
        }
    }
}
