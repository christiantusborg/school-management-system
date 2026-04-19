using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPathwayDocumentsAndProgrammePathways : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PathwayDocumentRequirements",
                columns: table => new
                {
                    PathwayDocumentRequirementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PathwayId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathwayDocumentRequirements", x => x.PathwayDocumentRequirementId);
                    table.ForeignKey(
                        name: "FK_PathwayDocumentRequirements_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PathwayDocumentRequirements_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammePathways",
                columns: table => new
                {
                    ProgrammePathwayId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PathwayId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammePathways", x => x.ProgrammePathwayId);
                    table.ForeignKey(
                        name: "FK_ProgrammePathways_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammePathways_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PathwayDocumentRequirements_DocumentTypeId",
                table: "PathwayDocumentRequirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PathwayDocumentRequirements_PathwayId_DocumentTypeId",
                table: "PathwayDocumentRequirements",
                columns: new[] { "PathwayId", "DocumentTypeId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePathways_PathwayId",
                table: "ProgrammePathways",
                column: "PathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammePathways_ProgrammeId_PathwayId",
                table: "ProgrammePathways",
                columns: new[] { "ProgrammeId", "PathwayId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PathwayDocumentRequirements");

            migrationBuilder.DropTable(
                name: "ProgrammePathways");
        }
    }
}
