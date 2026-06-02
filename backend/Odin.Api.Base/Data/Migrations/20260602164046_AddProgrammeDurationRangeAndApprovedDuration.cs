using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeDurationRangeAndApprovedDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxDurationMonths",
                table: "Programmes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinDurationMonths",
                table: "Programmes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedDurationMonths",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            // Backfill min and max from each programme's specialisations so
            // existing rows already satisfy `min <= value <= max`. Both bounds
            // start at the same value; the admin widens the range from the
            // programme-setup UI when they're ready.
            migrationBuilder.Sql(@"
UPDATE ""Programmes"" p
SET ""MaxDurationMonths"" = COALESCE(sub.max_months, 0),
    ""MinDurationMonths"" = COALESCE(sub.max_months, 0)
FROM (
    SELECT ""ProgrammeId"", MAX(""DurationOfStudyMonths"") AS max_months
    FROM ""Specializations""
    WHERE ""DeletedAt"" IS NULL
    GROUP BY ""ProgrammeId""
) sub
WHERE p.""ProgrammeId"" = sub.""ProgrammeId"";
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDurationMonths",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "MinDurationMonths",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "ApprovedDurationMonths",
                table: "Enrollments");
        }
    }
}
