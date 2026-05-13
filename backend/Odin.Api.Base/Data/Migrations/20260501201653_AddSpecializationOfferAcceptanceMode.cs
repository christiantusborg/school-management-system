using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecializationOfferAcceptanceMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfferAcceptanceMode",
                table: "Specializations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "StudentAccept");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferAcceptanceMode",
                table: "Specializations");
        }
    }
}
