using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalProjectUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "AssignmentUploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "AssignmentUploads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "AssignmentUploads",
                type: "integer",
                nullable: true);

            // Supersedes FinalProjectCohortFields (same day, never used): the
            // final-project details moved from cohort-level builder fields to
            // the per-student assignment uploads above, so take the three
            // cohort-type fields (and any values) back out.
            migrationBuilder.Sql("""
                DELETE FROM "CohortFieldValues" v
                USING "CohortTypeFields" f, "CohortTypes" t
                WHERE v."CohortTypeFieldId" = f."CohortTypeFieldId"
                  AND f."CohortTypeId" = t."CohortTypeId"
                  AND t."Name" ILIKE '%final project%'
                  AND f."Label" IN ('Final project name', 'Supervisor', 'Word count');
                DELETE FROM "CohortTypeFields" f
                USING "CohortTypes" t
                WHERE f."CohortTypeId" = t."CohortTypeId"
                  AND t."Name" ILIKE '%final project%'
                  AND f."Label" IN ('Final project name', 'Supervisor', 'Word count');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "AssignmentUploads");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "AssignmentUploads");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "AssignmentUploads");
        }
    }
}
