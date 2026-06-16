using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentLetterReferenceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LetterReferenceCode",
                table: "Enrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LetterReferenceCode",
                table: "Enrollments",
                column: "LetterReferenceCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LetterReferenceCode",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "LetterReferenceCode",
                table: "Enrollments");
        }
    }
}
