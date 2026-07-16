using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentScanColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AiConfidence",
                table: "StudentDocuments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AiFraudRisk",
                table: "StudentDocuments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiResult",
                table: "StudentDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrResult",
                table: "StudentDocuments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiConfidence",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "AiFraudRisk",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "AiResult",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "OcrResult",
                table: "StudentDocuments");
        }
    }
}
