using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpecializationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerSpecializationStatuses",
                columns: table => new
                {
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerSpecializationStatuses", x => x.SpecializationId);
                    table.ForeignKey(
                        name: "FK_PartnerSpecializationStatuses_Specializations_Specializatio~",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "SpecializationId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Approval moves from programme level to specialization level:
            // every spec of a partner-owned programme inherits the programme's
            // current workflow status so nothing changes for live students.
            migrationBuilder.Sql("""
                INSERT INTO "PartnerSpecializationStatuses"
                    ("SpecializationId", "Status", "RejectionReason", "UpdatedAt")
                SELECT s."SpecializationId",
                       COALESCE(ps."Status", 0),
                       ps."RejectionReason",
                       now()
                FROM "Specializations" s
                JOIN "Programmes" p ON p."ProgrammeId" = s."ProgrammeId"
                    AND p."OwnerId" IS NOT NULL
                LEFT JOIN "PartnerProgrammeStatuses" ps ON ps."ProgrammeId" = p."ProgrammeId"
                WHERE s."DeletedAt" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerSpecializationStatuses");
        }
    }
}
