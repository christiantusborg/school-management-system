using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentStatusFlowFormalize : Migration
    {
        // Mirrors EnrollmentStatusIds.cs. Hard-coded so this migration is
        // self-contained and reproducible without runtime constants.
        private const string Draft                                = "22222222-2222-2222-2222-200000000001";
        private const string ApplicationAwaitingReviewByPartner   = "22222222-2222-2222-2222-200000000002";
        private const string ApplicationRejectedByPartner         = "22222222-2222-2222-2222-200000000003";
        private const string ApplicationAwaitingReviewByAdmission = "22222222-2222-2222-2222-200000000004";
        private const string ApplicationRejectedByAdmission       = "22222222-2222-2222-2222-200000000005";
        private const string ApplicationApprovedAdmission         = "22222222-2222-2222-2222-200000000006";
        private const string AcceptAdmission                      = "22222222-2222-2222-2222-200000000007";
        private const string GradesApproved                       = "22222222-2222-2222-2222-200000000008";
        private const string ApplicationSubmitted                 = "22222222-2222-2222-2222-200000000009";
        private const string AcceptOffer                          = "22222222-2222-2222-2222-20000000000a";
        private const string AwaitingGradesSubmit                 = "22222222-2222-2222-2222-20000000000b";
        private const string AwaitingGradesApproval               = "22222222-2222-2222-2222-20000000000c";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add columns nullable so the renames-in-place can run before
            //    the FK target rows have their values populated.
            migrationBuilder.AddColumn<string>(
                name: "NextActionRole",
                table: "EnrollmentStatuses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NextStatusOnCompleteId",
                table: "EnrollmentStatuses",
                type: "uuid",
                nullable: true);

            // 2. Insert the 4 new rows (with no transitions yet — set in step 3).
            migrationBuilder.Sql($@"
                INSERT INTO ""EnrollmentStatuses""
                       (""EnrollmentStatusId"", ""Code"", ""Name"", ""Level"", ""LevelDown"", ""DeletedAt"") VALUES
                  ('{ApplicationSubmitted}',   'ApplicationSubmitted',   'Application Submitted',     50,   0, NULL),
                  ('{AcceptOffer}',            'AcceptOffer',            'Accept Offer',             250, 200, NULL),
                  ('{AwaitingGradesSubmit}',   'AwaitingGradesSubmit',   'Awaiting Grades Submit',   400, 350, NULL),
                  ('{AwaitingGradesApproval}', 'AwaitingGradesApproval', 'Awaiting Grades Approval', 450, 400, NULL);
            ");

            // 3. Rename existing 8 rows + set NextActionRole + NextStatusOnCompleteId
            //    on all 12 in a single batch.
            migrationBuilder.Sql($@"
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'Draft',                                ""Name"" = 'Draft',                                ""Level"" =   0, ""LevelDown"" =   0, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{ApplicationSubmitted}'::uuid                 WHERE ""EnrollmentStatusId"" = '{Draft}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationSubmitted',                 ""Name"" = 'Application Submitted',                ""Level"" =  50, ""LevelDown"" =   0, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{ApplicationAwaitingReviewByPartner}'::uuid   WHERE ""EnrollmentStatusId"" = '{ApplicationSubmitted}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationRejectedByPartner',         ""Name"" = 'Application Rejected by Partner',      ""Level"" =  75, ""LevelDown"" =   0, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{ApplicationAwaitingReviewByPartner}'::uuid   WHERE ""EnrollmentStatusId"" = '{ApplicationRejectedByPartner}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationRejectedByAdmission',       ""Name"" = 'Application Rejected by Admission',    ""Level"" =  85, ""LevelDown"" =   0, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{ApplicationAwaitingReviewByPartner}'::uuid   WHERE ""EnrollmentStatusId"" = '{ApplicationRejectedByAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationAwaitingReviewByPartner',   ""Name"" = 'Awaiting Review by Partner',           ""Level"" = 100, ""LevelDown"" =  50, ""NextActionRole"" = 'Partner',   ""NextStatusOnCompleteId"" = '{ApplicationAwaitingReviewByAdmission}'::uuid WHERE ""EnrollmentStatusId"" = '{ApplicationAwaitingReviewByPartner}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationAwaitingReviewByAdmission', ""Name"" = 'Awaiting Review by Admission',         ""Level"" = 200, ""LevelDown"" = 100, ""NextActionRole"" = 'Admission', ""NextStatusOnCompleteId"" = '{AcceptOffer}'::uuid                          WHERE ""EnrollmentStatusId"" = '{ApplicationAwaitingReviewByAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'AcceptOffer',                          ""Name"" = 'Accept Offer',                         ""Level"" = 250, ""LevelDown"" = 200, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{ApplicationApprovedAdmission}'::uuid         WHERE ""EnrollmentStatusId"" = '{AcceptOffer}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApplicationApprovedAdmission',         ""Name"" = 'Approved by Admission',                ""Level"" = 300, ""LevelDown"" = 250, ""NextActionRole"" = 'Admission', ""NextStatusOnCompleteId"" = '{AcceptAdmission}'::uuid                      WHERE ""EnrollmentStatusId"" = '{ApplicationApprovedAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'AcceptAdmission',                      ""Name"" = 'Accept Admission',                     ""Level"" = 350, ""LevelDown"" = 300, ""NextActionRole"" = 'Student',   ""NextStatusOnCompleteId"" = '{AwaitingGradesSubmit}'::uuid                 WHERE ""EnrollmentStatusId"" = '{AcceptAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'AwaitingGradesSubmit',                 ""Name"" = 'Awaiting Grades Submit',               ""Level"" = 400, ""LevelDown"" = 350, ""NextActionRole"" = 'Partner',   ""NextStatusOnCompleteId"" = '{AwaitingGradesApproval}'::uuid               WHERE ""EnrollmentStatusId"" = '{AwaitingGradesSubmit}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'AwaitingGradesApproval',               ""Name"" = 'Awaiting Grades Approval',             ""Level"" = 450, ""LevelDown"" = 400, ""NextActionRole"" = 'Admission', ""NextStatusOnCompleteId"" = '{GradesApproved}'::uuid                       WHERE ""EnrollmentStatusId"" = '{AwaitingGradesApproval}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'GradesApproved',                       ""Name"" = 'Grades Approved',                      ""Level"" = 500, ""LevelDown"" = 450, ""NextActionRole"" = NULL,        ""NextStatusOnCompleteId"" = NULL                                            WHERE ""EnrollmentStatusId"" = '{GradesApproved}'::uuid;
            ");

            // 4. Index + FK on the new self-ref column.
            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentStatuses_NextStatusOnCompleteId",
                table: "EnrollmentStatuses",
                column: "NextStatusOnCompleteId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentStatuses_EnrollmentStatuses_NextStatusOnCompleteId",
                table: "EnrollmentStatuses",
                column: "NextStatusOnCompleteId",
                principalTable: "EnrollmentStatuses",
                principalColumn: "EnrollmentStatusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentStatuses_EnrollmentStatuses_NextStatusOnCompleteId",
                table: "EnrollmentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_EnrollmentStatuses_NextStatusOnCompleteId",
                table: "EnrollmentStatuses");

            // Restore old Codes/Names so a Down() leaves the previous state intact.
            migrationBuilder.Sql($@"
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'Submitted',                 ""Name"" = 'Submitted'                       WHERE ""EnrollmentStatusId"" = '{ApplicationAwaitingReviewByPartner}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'DocumentRejectedByPartner', ""Name"" = 'Document Rejected by Partner'    WHERE ""EnrollmentStatusId"" = '{ApplicationRejectedByPartner}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ReviewedByPartner',         ""Name"" = 'Reviewed by Partner'             WHERE ""EnrollmentStatusId"" = '{ApplicationAwaitingReviewByAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'RejectedByEnrolment',       ""Name"" = 'Rejected by IBSS'                WHERE ""EnrollmentStatusId"" = '{ApplicationRejectedByAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'ApprovedByEnrolment',       ""Name"" = 'Approved by IBSS'                WHERE ""EnrollmentStatusId"" = '{ApplicationApprovedAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'Active',                    ""Name"" = 'Active'                          WHERE ""EnrollmentStatusId"" = '{AcceptAdmission}'::uuid;
                UPDATE ""EnrollmentStatuses"" SET ""Code"" = 'Graduated',                 ""Name"" = 'Graduated'                       WHERE ""EnrollmentStatusId"" = '{GradesApproved}'::uuid;
                DELETE FROM ""EnrollmentStatuses"" WHERE ""EnrollmentStatusId"" IN
                  ('{ApplicationSubmitted}'::uuid,
                   '{AcceptOffer}'::uuid,
                   '{AwaitingGradesSubmit}'::uuid,
                   '{AwaitingGradesApproval}'::uuid);
            ");

            migrationBuilder.DropColumn(
                name: "NextActionRole",
                table: "EnrollmentStatuses");

            migrationBuilder.DropColumn(
                name: "NextStatusOnCompleteId",
                table: "EnrollmentStatuses");
        }
    }
}
