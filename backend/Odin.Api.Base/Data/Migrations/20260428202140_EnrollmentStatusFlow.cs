using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentStatusFlow : Migration
    {
        // Mirrors SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds. Hard-coded
        // here because migrations must be self-contained and reproducible without
        // referencing live runtime constants.
        private const string Draft                     = "22222222-2222-2222-2222-200000000001";
        private const string Submitted                 = "22222222-2222-2222-2222-200000000002";
        private const string DocumentRejectedByPartner = "22222222-2222-2222-2222-200000000003";
        private const string ReviewedByPartner         = "22222222-2222-2222-2222-200000000004";
        private const string RejectedByEnrolment       = "22222222-2222-2222-2222-200000000005";
        private const string ApprovedByEnrolment       = "22222222-2222-2222-2222-200000000006";
        private const string Active                    = "22222222-2222-2222-2222-200000000007";
        private const string Graduated                 = "22222222-2222-2222-2222-200000000008";

        // DocumentStatusIds (from earlier migration); used to update Level/LevelDown.
        private const string DocSubmitted           = "11111111-1111-1111-1111-100000000001";
        private const string DocVerifiedByPartner   = "11111111-1111-1111-1111-100000000002";
        private const string DocVerifiedByEnrolment = "11111111-1111-1111-1111-100000000003";
        private const string DocRejectedByPartner   = "11111111-1111-1111-1111-100000000004";
        private const string DocRejectedByEnrolment = "11111111-1111-1111-1111-100000000005";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. EnrollmentStatuses lookup table.
            migrationBuilder.CreateTable(
                name: "EnrollmentStatuses",
                columns: table => new
                {
                    EnrollmentStatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LevelDown = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentStatuses", x => x.EnrollmentStatusId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatuses_Code",
                table: "EnrollmentStatuses",
                column: "Code",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            // 2. Seed enrollment statuses so subsequent FK creation succeeds.
            migrationBuilder.Sql($@"
                INSERT INTO ""EnrollmentStatuses"" (""EnrollmentStatusId"", ""Code"", ""Name"", ""Level"", ""LevelDown"", ""DeletedAt"") VALUES
                  ('{Draft}',                     'Draft',                     'Draft',                          0,   0, NULL),
                  ('{Submitted}',                 'Submitted',                 'Submitted',                    100,   0, NULL),
                  ('{DocumentRejectedByPartner}', 'DocumentRejectedByPartner', 'Document Rejected by Partner', 110,   0, NULL),
                  ('{ReviewedByPartner}',         'ReviewedByPartner',         'Reviewed by Partner',          200, 100, NULL),
                  ('{RejectedByEnrolment}',       'RejectedByEnrolment',       'Rejected by IBSS',             210, 100, NULL),
                  ('{ApprovedByEnrolment}',       'ApprovedByEnrolment',       'Approved by IBSS',             300, 200, NULL),
                  ('{Active}',                    'Active',                    'Active',                       400, 300, NULL),
                  ('{Graduated}',                 'Graduated',                 'Graduated',                    500, 400, NULL);
            ");

            // 3. DocumentStatuses gets Level + LevelDown columns + values.
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "DocumentStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevelDown",
                table: "DocumentStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql($@"
                UPDATE ""DocumentStatuses"" SET ""Level"" = 100, ""LevelDown"" =   0 WHERE ""DocumentStatusId"" = '{DocSubmitted}'::uuid;
                UPDATE ""DocumentStatuses"" SET ""Level"" = 200, ""LevelDown"" = 100 WHERE ""DocumentStatusId"" = '{DocVerifiedByPartner}'::uuid;
                UPDATE ""DocumentStatuses"" SET ""Level"" = 150, ""LevelDown"" =   0 WHERE ""DocumentStatusId"" = '{DocRejectedByPartner}'::uuid;
                UPDATE ""DocumentStatuses"" SET ""Level"" = 300, ""LevelDown"" = 200 WHERE ""DocumentStatusId"" = '{DocVerifiedByEnrolment}'::uuid;
                UPDATE ""DocumentStatuses"" SET ""Level"" = 250, ""LevelDown"" = 100 WHERE ""DocumentStatusId"" = '{DocRejectedByEnrolment}'::uuid;
            ");

            // 4. Enrollments.StatusId nullable column for backfill.
            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            // 5. Backfill StatusId based on the existing timestamps and old
            //    AdditionalStatueId (which only ever held "Document Rejected").
            migrationBuilder.Sql($@"
                UPDATE ""Enrollments""
                SET ""StatusId"" = CASE
                    WHEN ""GraduatedAt""              IS NOT NULL THEN '{Graduated}'::uuid
                    WHEN ""AdmissionConfirmedAt""     IS NOT NULL THEN '{Active}'::uuid
                    WHEN ""AdditionalStatueId""       IS NOT NULL THEN '{DocumentRejectedByPartner}'::uuid
                    WHEN ""ApplicationReviewedAt""    IS NOT NULL THEN '{ReviewedByPartner}'::uuid
                    WHEN ""ApplicationSubmittedAt"" >  '0001-01-02 00:00:00' THEN '{Submitted}'::uuid
                    ELSE '{Draft}'::uuid
                END;
            ");

            // 6. NOT NULL + FK + index.
            migrationBuilder.AlterColumn<Guid>(
                name: "StatusId",
                table: "Enrollments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StatusId",
                table: "Enrollments",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_EnrollmentStatuses_StatusId",
                table: "Enrollments",
                column: "StatusId",
                principalTable: "EnrollmentStatuses",
                principalColumn: "EnrollmentStatusId",
                onDelete: ReferentialAction.Restrict);

            // 7. Drop the FK + column to AdditionalEnrollmentStatues.
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AdditionalEnrollmentStatues_AdditionalStatueId",
                table: "Enrollments");
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_AdditionalStatueId",
                table: "Enrollments");
            migrationBuilder.DropColumn(
                name: "AdditionalStatueId",
                table: "Enrollments");

            // 8. Drop the seven legacy timestamp columns from Enrollments.
            migrationBuilder.DropColumn(name: "ApplicationSubmittedAt",     table: "Enrollments");
            migrationBuilder.DropColumn(name: "ApplicationReviewedAt",      table: "Enrollments");
            migrationBuilder.DropColumn(name: "ApplicationOfferAcceptedAt", table: "Enrollments");
            migrationBuilder.DropColumn(name: "AdmissionConfirmedAt",       table: "Enrollments");
            migrationBuilder.DropColumn(name: "GradesSubmittedAt",          table: "Enrollments");
            migrationBuilder.DropColumn(name: "GraduatedAt",                table: "Enrollments");
            migrationBuilder.DropColumn(name: "GraduationApprovedAt",       table: "Enrollments");

            // 9. Drop the obsolete AdditionalEnrollmentStatues table.
            migrationBuilder.DropTable(name: "AdditionalEnrollmentStatues");

            // 10. EnrollmentStatusNotes: add StatusId + FK + index.
            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                table: "EnrollmentStatusNotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($@"
                UPDATE ""EnrollmentStatusNotes""
                SET ""StatusId"" = '{DocumentRejectedByPartner}'::uuid
                WHERE ""StatusId"" IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatusNotes_StatusId",
                table: "EnrollmentStatusNotes",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentStatusNotes_EnrollmentStatuses_StatusId",
                table: "EnrollmentStatusNotes",
                column: "StatusId",
                principalTable: "EnrollmentStatuses",
                principalColumn: "EnrollmentStatusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-create AdditionalEnrollmentStatues table (lossy — original Guids lost).
            migrationBuilder.CreateTable(
                name: "AdditionalEnrollmentStatues",
                columns: table => new
                {
                    AdditionalStatueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalEnrollmentStatues", x => x.AdditionalStatueId);
                });

            // Re-create the seven Enrollments timestamps + AdditionalStatueId FK.
            migrationBuilder.AddColumn<DateTime>(name: "ApplicationSubmittedAt",     table: "Enrollments", type: "timestamp without time zone", nullable: false, defaultValueSql: "'0001-01-01 00:00:00'");
            migrationBuilder.AddColumn<DateTime>(name: "ApplicationReviewedAt",      table: "Enrollments", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ApplicationOfferAcceptedAt", table: "Enrollments", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "AdmissionConfirmedAt",       table: "Enrollments", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "GradesSubmittedAt",          table: "Enrollments", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "GraduatedAt",                table: "Enrollments", type: "timestamp without time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "GraduationApprovedAt",       table: "Enrollments", type: "timestamp without time zone", nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdditionalStatueId",
                table: "Enrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_AdditionalStatueId",
                table: "Enrollments",
                column: "AdditionalStatueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AdditionalEnrollmentStatues_AdditionalStatueId",
                table: "Enrollments",
                column: "AdditionalStatueId",
                principalTable: "AdditionalEnrollmentStatues",
                principalColumn: "AdditionalStatueId",
                onDelete: ReferentialAction.Restrict);

            // Drop FK + column on Enrollments.StatusId.
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_EnrollmentStatuses_StatusId",
                table: "Enrollments");
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StatusId",
                table: "Enrollments");
            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Enrollments");

            // EnrollmentStatusNotes.StatusId.
            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentStatusNotes_EnrollmentStatuses_StatusId",
                table: "EnrollmentStatusNotes");
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentStatusNotes_StatusId",
                table: "EnrollmentStatusNotes");
            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "EnrollmentStatusNotes");

            // Drop DocumentStatuses Level/LevelDown.
            migrationBuilder.DropColumn(name: "Level",     table: "DocumentStatuses");
            migrationBuilder.DropColumn(name: "LevelDown", table: "DocumentStatuses");

            // Drop EnrollmentStatuses table.
            migrationBuilder.DropTable(name: "EnrollmentStatuses");
        }
    }
}
