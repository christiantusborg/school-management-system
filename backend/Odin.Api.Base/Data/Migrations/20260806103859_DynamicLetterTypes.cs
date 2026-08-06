using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class DynamicLetterTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "LetterTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LetterTypeDefinitionId",
                table: "LetterTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LetterLanguages",
                columns: table => new
                {
                    LetterLanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterLanguages", x => x.LetterLanguageId);
                });

            migrationBuilder.CreateTable(
                name: "LetterTypeDefinitions",
                columns: table => new
                {
                    LetterTypeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ReferencePrefix = table.Column<string>(type: "text", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerStatusId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisibleToStudent = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleToPartner = table.Column<bool>(type: "boolean", nullable: false),
                    EmailOnRelease = table.Column<bool>(type: "boolean", nullable: false),
                    AllowLegacyUpload = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterTypeDefinitions", x => x.LetterTypeDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "StudentDocumentVersions",
                columns: table => new
                {
                    StudentDocumentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    GeneratedByName = table.Column<string>(type: "text", nullable: true),
                    GeneratedByUserId = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocumentVersions", x => x.StudentDocumentVersionId);
                });

            // Seed the four letter types the Admission Office asked for, each
            // with its own system-generated DocumentType and the matching
            // status trigger. All settings editable in System Config later.
            migrationBuilder.Sql("""
                INSERT INTO "DocumentTypes" ("DocumentTypeId", "Name", "Description", "IsSystemGenerated")
                VALUES
                    ('22222222-2222-2222-2222-110000000001', 'Verification Letter (Active Student)', 'System-generated verification letter PDF.', TRUE),
                    ('22222222-2222-2222-2222-110000000002', 'Graduation Confirmation Letter',       'System-generated graduation confirmation PDF.', TRUE),
                    ('22222222-2222-2222-2222-110000000003', 'Deferred Letter',                      'System-generated deferral letter PDF.', TRUE),
                    ('22222222-2222-2222-2222-110000000004', 'Drop-out Letter',                      'System-generated drop-out letter PDF.', TRUE)
                ON CONFLICT ("DocumentTypeId") DO NOTHING;

                INSERT INTO "LetterTypeDefinitions"
                    ("LetterTypeDefinitionId", "Name", "ReferencePrefix", "DocumentTypeId", "TriggerStatusId",
                     "VisibleToStudent", "VisibleToPartner", "EmailOnRelease", "AllowLegacyUpload", "SortOrder", "CreatedAt")
                VALUES
                    ('22222222-2222-2222-2222-120000000001', 'Verification Letter (Active Student)', 'VL',
                     '22222222-2222-2222-2222-110000000001', '22222222-2222-2222-2222-200000000007', TRUE, TRUE, FALSE, FALSE, 1, NOW()),
                    ('22222222-2222-2222-2222-120000000002', 'Graduation Confirmation Letter', 'GC',
                     '22222222-2222-2222-2222-110000000002', '22222222-2222-2222-2222-200000000008', TRUE, TRUE, FALSE, FALSE, 2, NOW()),
                    ('22222222-2222-2222-2222-120000000003', 'Deferred Letter', 'DF',
                     '22222222-2222-2222-2222-110000000003', '22222222-2222-2222-2222-20000000000d', TRUE, TRUE, FALSE, FALSE, 3, NOW()),
                    ('22222222-2222-2222-2222-120000000004', 'Drop-out Letter', 'DO',
                     '22222222-2222-2222-2222-110000000004', '22222222-2222-2222-2222-20000000000e', TRUE, TRUE, FALSE, FALSE, 4, NOW())
                ON CONFLICT ("LetterTypeDefinitionId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetterLanguages");

            migrationBuilder.DropTable(
                name: "LetterTypeDefinitions");

            migrationBuilder.DropTable(
                name: "StudentDocumentVersions");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "LetterTemplates");

            migrationBuilder.DropColumn(
                name: "LetterTypeDefinitionId",
                table: "LetterTemplates");
        }
    }
}
