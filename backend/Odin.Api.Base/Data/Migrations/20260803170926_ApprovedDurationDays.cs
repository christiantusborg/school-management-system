using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApprovedDurationDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedDurationMonths",
                table: "Enrollments",
                newName: "ApprovedDurationDays");

            // Backfill: the renamed column still holds MONTHS — convert every
            // value to DAYS using the enrolment's real calendar (commencement
            // + N months), falling back to the 30.44-day average when no
            // commencement date exists.
            migrationBuilder.Sql("""
                UPDATE "Enrollments" SET "ApprovedDurationDays" = CASE
                    WHEN "ApprovedDurationDays" IS NULL THEN NULL
                    WHEN "CommencementDate" IS NOT NULL THEN
                        ("CommencementDate"::date + make_interval(months => "ApprovedDurationDays"))::date - "CommencementDate"::date
                    ELSE ROUND("ApprovedDurationDays" * 30.44)::int
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort reverse: days back to the nearest month count.
            migrationBuilder.Sql("""
                UPDATE "Enrollments" SET "ApprovedDurationDays" =
                    CASE WHEN "ApprovedDurationDays" IS NULL THEN NULL
                         ELSE GREATEST(1, ROUND("ApprovedDurationDays" / 30.44))::int END;
                """);
            migrationBuilder.RenameColumn(
                name: "ApprovedDurationDays",
                table: "Enrollments",
                newName: "ApprovedDurationMonths");
        }
    }
}
