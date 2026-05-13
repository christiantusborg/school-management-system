using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterTemplatesAndSystemDocTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "DocumentTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LetterTemplates",
                columns: table => new
                {
                    LetterTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LetterType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    CertificateBackgroundPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CertificateLayoutJson = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterTemplates", x => x.LetterTemplateId);
                    table.ForeignKey(
                        name: "FK_LetterTemplates_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_LetterType",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetterTemplates");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "DocumentTypes");
        }
    }
}
