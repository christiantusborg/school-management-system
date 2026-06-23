using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIsLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyStudent",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLegacyStudent",
                table: "Students");
        }
    }
}
