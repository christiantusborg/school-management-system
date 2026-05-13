using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStudentDocumentStatuses : Migration
    {
        // Mirrors SharedLibrary.Basics.Opaque.Domains.DocumentStatusIds. Hard-coded
        // here because migrations must be self-contained and reproducible without
        // referencing live runtime constants.
        private const string Submitted           = "11111111-1111-1111-1111-100000000001";
        private const string VerifiedByPartner   = "11111111-1111-1111-1111-100000000002";
        private const string VerifiedByEnrolment = "11111111-1111-1111-1111-100000000003";
        private const string RejectedByPartner   = "11111111-1111-1111-1111-100000000004";
        private const string RejectedByEnrolment = "11111111-1111-1111-1111-100000000005";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Lookup table.
            migrationBuilder.CreateTable(
                name: "DocumentStatuses",
                columns: table => new
                {
                    DocumentStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStatuses", x => x.DocumentStatusId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStatuses_Code",
                table: "DocumentStatuses",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            // 2. Seed lookup rows so subsequent FK creations succeed.
            migrationBuilder.Sql($@"
                INSERT INTO ""DocumentStatuses"" (""DocumentStatusId"", ""Code"", ""Name"", ""DeletedAt"") VALUES
                  ('{Submitted}',           'Submitted',           'Submitted',            NULL),
                  ('{VerifiedByPartner}',   'VerifiedByPartner',   'Verified by Partner',  NULL),
                  ('{VerifiedByEnrolment}', 'VerifiedByEnrolment', 'Verified by IBSS',     NULL),
                  ('{RejectedByPartner}',   'RejectedByPartner',   'Rejected by Partner',  NULL),
                  ('{RejectedByEnrolment}', 'RejectedByEnrolment', 'Rejected by IBSS',     NULL);
            ");

            // 3. Add CurrentStatusId nullable for the backfill.
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentStatusId",
                table: "StudentDocuments",
                type: "uuid",
                nullable: true);

            // 4. Backfill CurrentStatusId from the old timestamps.
            //    VerifiedByEnrolmentAt wins over VerifiedByPartnerAt because IBSS
            //    is the higher authority. NULL → Submitted.
            migrationBuilder.Sql($@"
                UPDATE ""StudentDocuments""
                SET ""CurrentStatusId"" = CASE
                    WHEN ""VerifiedByEnrolmentAt"" IS NOT NULL THEN '{VerifiedByEnrolment}'::uuid
                    WHEN ""VerifiedByPartnerAt""   IS NOT NULL THEN '{VerifiedByPartner}'::uuid
                    ELSE '{Submitted}'::uuid
                END
                WHERE ""DeletedAt"" IS NULL OR ""DeletedAt"" IS NOT NULL;
            ");

            // 5. Make CurrentStatusId non-null now that every row has a value.
            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentStatusId",
                table: "StudentDocuments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 6. Notes table.
            migrationBuilder.CreateTable(
                name: "StudentDocumentNotes",
                columns: table => new
                {
                    StudentDocumentNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    ByUserId = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocumentNotes", x => x.StudentDocumentNoteId);
                    table.ForeignKey(
                        name: "FK_StudentDocumentNotes_DocumentStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "DocumentStatuses",
                        principalColumn: "DocumentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDocumentNotes_StudentDocuments_StudentDocumentId",
                        column: x => x.StudentDocumentId,
                        principalTable: "StudentDocuments",
                        principalColumn: "StudentDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            // 7. Backfill one note per legacy document so history isn't empty.
            //    Status follows CurrentStatusId; ByUserId is the student's owning
            //    user; CreatedAt is the upload time we already have.
            migrationBuilder.Sql(@"
                INSERT INTO ""StudentDocumentNotes""
                       (""StudentDocumentNoteId"", ""StudentDocumentId"", ""StatusId"", ""ByUserId"", ""Note"", ""CreatedAt"")
                SELECT gen_random_uuid(),
                       sd.""StudentDocumentId"",
                       sd.""CurrentStatusId"",
                       s.""UserId"",
                       NULL,
                       sd.""UploadedAt""
                FROM ""StudentDocuments"" sd
                JOIN ""Students"" s ON s.""StudentId"" = sd.""StudentId"";
            ");

            // 8. Final indices + FK to lookup.
            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_CurrentStatusId",
                table: "StudentDocuments",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocumentNotes_StatusId",
                table: "StudentDocumentNotes",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocumentNotes_StudentDocumentId",
                table: "StudentDocumentNotes",
                column: "StudentDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDocuments_DocumentStatuses_CurrentStatusId",
                table: "StudentDocuments",
                column: "CurrentStatusId",
                principalTable: "DocumentStatuses",
                principalColumn: "DocumentStatusId",
                onDelete: ReferentialAction.Restrict);

            // 9. Drop the legacy timestamp columns.
            migrationBuilder.DropColumn(
                name: "VerifiedByEnrolmentAt",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "VerifiedByPartnerAt",
                table: "StudentDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedByEnrolmentAt",
                table: "StudentDocuments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedByPartnerAt",
                table: "StudentDocuments",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.Sql($@"
                UPDATE ""StudentDocuments""
                SET ""VerifiedByPartnerAt""   = ""UploadedAt"" WHERE ""CurrentStatusId"" = '{VerifiedByPartner}'::uuid;
                UPDATE ""StudentDocuments""
                SET ""VerifiedByEnrolmentAt"" = ""UploadedAt"" WHERE ""CurrentStatusId"" = '{VerifiedByEnrolment}'::uuid;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentDocuments_DocumentStatuses_CurrentStatusId",
                table: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "StudentDocumentNotes");

            migrationBuilder.DropTable(
                name: "DocumentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_StudentDocuments_CurrentStatusId",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "StudentDocuments");
        }
    }
}
