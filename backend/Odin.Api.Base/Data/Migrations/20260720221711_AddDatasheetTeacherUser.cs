using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatasheetTeacherUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeacherUserId",
                table: "PartnerDatasheets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheets_TeacherUserId",
                table: "PartnerDatasheets",
                column: "TeacherUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartnerDatasheets_TeacherUserId",
                table: "PartnerDatasheets");

            migrationBuilder.DropColumn(
                name: "TeacherUserId",
                table: "PartnerDatasheets");
        }
    }
}
