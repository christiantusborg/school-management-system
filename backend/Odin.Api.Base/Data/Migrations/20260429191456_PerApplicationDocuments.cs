using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerApplicationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Per-application columns on StudentDocuments. Both nullable
            //    in DB — semantic enforcement happens in code (reviewer
            //    endpoints assert EnrollmentId IS NOT NULL).
            migrationBuilder.AddColumn<Guid>(
                name: "EnrollmentId",
                table: "StudentDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignupSpecializationId",
                table: "StudentDocuments",
                type: "uuid",
                nullable: true);

            // 2. New ProgrammeDocumentRequirement table.
            migrationBuilder.CreateTable(
                name: "ProgrammeDocumentRequirements",
                columns: table => new
                {
                    ProgrammeDocumentRequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeDocumentRequirements", x => x.ProgrammeDocumentRequirementId);
                    table.ForeignKey(
                        name: "FK_ProgrammeDocumentRequirements_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammeDocumentRequirements_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_EnrollmentId_DocumentTypeId",
                table: "StudentDocuments",
                columns: new[] { "EnrollmentId", "DocumentTypeId" },
                unique: true,
                filter: "\"EnrollmentId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeDocumentRequirements_DocumentTypeId",
                table: "ProgrammeDocumentRequirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeDocumentRequirements_ProgrammeId_DocumentTypeId",
                table: "ProgrammeDocumentRequirements",
                columns: new[] { "ProgrammeId", "DocumentTypeId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDocuments_Enrollments_EnrollmentId",
                table: "StudentDocuments",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "StudentEnrollmentId",
                onDelete: ReferentialAction.Restrict);

            // 3. Seed ProgrammeDocumentRequirement rows: every Programme gets
            //    the existing canonical 4 doc-types (resolved by Name). The
            //    seeder then re-affirms this on every boot, but seeding here
            //    means the schema is self-bootstrapping for fresh DBs and
            //    immediately satisfies the resubmit / detail endpoints'
            //    "what's required for this programme" lookup.
            migrationBuilder.Sql(@"
                INSERT INTO ""ProgrammeDocumentRequirements""
                       (""ProgrammeDocumentRequirementId"", ""ProgrammeId"", ""DocumentTypeId"", ""DeletedAt"")
                SELECT gen_random_uuid(), p.""ProgrammeId"", dt.""DocumentTypeId"", NULL
                FROM ""Programmes"" p
                CROSS JOIN ""DocumentTypes"" dt
                WHERE p.""DeletedAt"" IS NULL
                  AND dt.""DeletedAt"" IS NULL
                  AND dt.""Name"" IN (
                      'Passport',
                      'Bachelor''s Degree Certificate',
                      'Language Proficiency Certificate',
                      'Curriculum Vitae'
                  )
                ON CONFLICT DO NOTHING;
            ");

            // 4. Backfill StudentDocuments: every active row × every active
            //    enrolment of its student gets a duplicate row tagged with
            //    EnrollmentId. The blob path is reused — the file content is
            //    identical, only the row is duplicated. Originals (with
            //    EnrollmentId still null) get soft-deleted at the end.
            migrationBuilder.Sql(@"
                INSERT INTO ""StudentDocuments""
                       (""StudentDocumentId"", ""StudentId"", ""DocumentTypeId"", ""EnrollmentId"",
                        ""SignupSpecializationId"", ""FileName"", ""MimeType"", ""UploadedAt"",
                        ""ExpiryDate"", ""StoragePath"", ""CurrentStatusId"", ""DeletedAt"")
                SELECT gen_random_uuid(),
                       sd.""StudentId"",
                       sd.""DocumentTypeId"",
                       e.""StudentEnrollmentId"",
                       NULL,
                       sd.""FileName"",
                       sd.""MimeType"",
                       sd.""UploadedAt"",
                       sd.""ExpiryDate"",
                       sd.""StoragePath"",
                       sd.""CurrentStatusId"",
                       NULL
                FROM ""StudentDocuments"" sd
                JOIN ""Enrollments"" e ON e.""StudentId"" = sd.""StudentId""
                                       AND e.""DeletedAt"" IS NULL
                WHERE sd.""DeletedAt"" IS NULL
                  AND sd.""EnrollmentId"" IS NULL;
            ");

            // 5. Soft-delete the now-orphan originals (StudentId-only rows).
            migrationBuilder.Sql(@"
                UPDATE ""StudentDocuments""
                SET ""DeletedAt"" = NOW() AT TIME ZONE 'UTC'
                WHERE ""EnrollmentId"" IS NULL
                  AND ""DeletedAt"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentDocuments_Enrollments_EnrollmentId",
                table: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "ProgrammeDocumentRequirements");

            migrationBuilder.DropIndex(
                name: "IX_StudentDocuments_EnrollmentId_DocumentTypeId",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "SignupSpecializationId",
                table: "StudentDocuments");
        }
    }
}
