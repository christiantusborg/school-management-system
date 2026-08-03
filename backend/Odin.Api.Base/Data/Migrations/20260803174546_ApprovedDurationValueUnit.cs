using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApprovedDurationValueUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedDurationDays",
                table: "Enrollments",
                newName: "ApprovedDurationValue");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedDurationUnit",
                table: "Enrollments",
                type: "text",
                nullable: true);

            // Restore the original entered intent. The previous migration
            // (ApprovedDurationDays) converted whole-month durations to days
            // (calendar-exact from commencement, else round(m * 30.44)). That
            // forward map is strictly increasing in m, so inverting it here
            // recovers the exact original month count. Anything that does not
            // map back to a whole month is a true day entry.
            migrationBuilder.Sql("""
                UPDATE "Enrollments" e
                SET "ApprovedDurationValue" = sub.m, "ApprovedDurationUnit" = 'Month'
                FROM (
                    SELECT e2."StudentEnrollmentId", gs.m
                    FROM "Enrollments" e2
                    JOIN LATERAL generate_series(1, 360) gs(m)
                      ON ((e2."CommencementDate"::date + make_interval(months => gs.m))::date
                            - e2."CommencementDate"::date) = e2."ApprovedDurationValue"
                    WHERE e2."ApprovedDurationValue" IS NOT NULL
                      AND e2."CommencementDate" IS NOT NULL
                ) sub
                WHERE e."StudentEnrollmentId" = sub."StudentEnrollmentId";

                UPDATE "Enrollments" e
                SET "ApprovedDurationValue" = sub.m, "ApprovedDurationUnit" = 'Month'
                FROM (
                    SELECT e2."StudentEnrollmentId", gs.m
                    FROM "Enrollments" e2
                    JOIN LATERAL generate_series(1, 360) gs(m)
                      ON ROUND(gs.m * 30.44)::int = e2."ApprovedDurationValue"
                    WHERE e2."ApprovedDurationValue" IS NOT NULL
                      AND e2."CommencementDate" IS NULL
                ) sub
                WHERE e."StudentEnrollmentId" = sub."StudentEnrollmentId"
                  AND e."ApprovedDurationUnit" IS NULL;

                UPDATE "Enrollments"
                SET "ApprovedDurationUnit" = 'Day'
                WHERE "ApprovedDurationValue" IS NOT NULL
                  AND "ApprovedDurationUnit" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to the days-only model: convert Month rows to their day
            // equivalent before the unit column disappears.
            migrationBuilder.Sql("""
                UPDATE "Enrollments"
                SET "ApprovedDurationValue" = CASE
                    WHEN "CommencementDate" IS NOT NULL THEN
                        ("CommencementDate"::date + make_interval(months => "ApprovedDurationValue"))::date
                            - "CommencementDate"::date
                    ELSE ROUND("ApprovedDurationValue" * 30.44)::int
                END
                WHERE "ApprovedDurationValue" IS NOT NULL
                  AND "ApprovedDurationUnit" = 'Month';
                """);

            migrationBuilder.DropColumn(
                name: "ApprovedDurationUnit",
                table: "Enrollments");

            migrationBuilder.RenameColumn(
                name: "ApprovedDurationValue",
                table: "Enrollments",
                newName: "ApprovedDurationDays");
        }
    }
}
