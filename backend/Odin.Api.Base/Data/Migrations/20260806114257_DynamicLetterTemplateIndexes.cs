using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class DynamicLetterTemplateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"LetterTypeDefinitionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterTypeDefinitionI~",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterTypeDefinitionId", "Language" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"LetterTypeDefinitionId\" IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"LetterTypeDefinitionId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterTypeDefini~",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterTypeDefinitionId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL AND \"LetterTypeDefinitionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterTypeDefinitionI~",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterTypeDefini~",
                table: "LetterEmailTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}
