using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerShortCodeAndFieldTooltip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "Partners",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tooltip",
                table: "PartnerDatasheetFields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tooltip",
                table: "FacultyProfileFields",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Tooltip",
                table: "PartnerDatasheetFields");

            migrationBuilder.DropColumn(
                name: "Tooltip",
                table: "FacultyProfileFields");
        }
    }
}
