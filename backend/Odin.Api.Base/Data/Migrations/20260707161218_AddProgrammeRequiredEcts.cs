using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeRequiredEcts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RequiredEcts",
                table: "Programmes",
                type: "numeric(7,1)",
                precision: 7,
                scale: 1,
                nullable: true);

            // Backfill each existing programme's completion threshold from its
            // own curriculum: the ECTS total of its fullest specialization (the
            // one that carries the whole subject set). This is always reachable
            // by a student who completes that specialization, unlike a textbook
            // level standard (e.g. 180) that could exceed the credits actually
            // on offer. Programmes with no subjects stay NULL (no gate).
            // Admission can adjust any programme afterwards.
            migrationBuilder.Sql(@"
                UPDATE ""Programmes"" p
                SET ""RequiredEcts"" = sub.max_ects
                FROM (
                    SELECT sp.""ProgrammeId"" AS pid, MAX(spec_sum.s) AS max_ects
                    FROM ""Specializations"" sp
                    JOIN LATERAL (
                        SELECT COALESCE(SUM(su.""Ects""), 0) AS s
                        FROM ""Subjects"" su
                        WHERE su.""SpecializationId"" = sp.""SpecializationId""
                          AND su.""DeletedAt"" IS NULL
                    ) spec_sum ON TRUE
                    WHERE sp.""DeletedAt"" IS NULL
                    GROUP BY sp.""ProgrammeId""
                ) sub
                WHERE p.""ProgrammeId"" = sub.pid AND sub.max_ects > 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredEcts",
                table: "Programmes");
        }
    }
}
