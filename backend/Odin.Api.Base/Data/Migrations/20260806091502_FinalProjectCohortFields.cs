using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <summary>
    /// Data-only migration: adds the Supervisor / Word count / Final project
    /// name builder fields to every "Final Project" cohort type (including
    /// clones), bypassing the UI lock on in-use types. The letter tag
    /// resolver reads these fields by label for the [supervisor],
    /// [word count] and [project title] tags, so the labels must stay
    /// exactly as inserted here.
    /// </summary>
    public partial class FinalProjectCohortFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "CohortTypeFields"
                    ("CohortTypeFieldId", "CohortTypeId", "Label", "Type", "OptionsText", "IsRequired", "SortOrder", "DeletedAt")
                SELECT gen_random_uuid(), t."CohortTypeId", v.label, v.type, NULL, FALSE,
                       COALESCE((SELECT MAX(f."SortOrder") FROM "CohortTypeFields" f
                                 WHERE f."CohortTypeId" = t."CohortTypeId"), 0) + v.ord,
                       NULL
                FROM "CohortTypes" t
                CROSS JOIN (VALUES
                    ('Final project name', 'text',   1),
                    ('Supervisor',         'text',   2),
                    ('Word count',         'number', 3)) AS v(label, type, ord)
                WHERE t."DeletedAt" IS NULL
                  AND t."Name" ILIKE '%final project%'
                  AND NOT EXISTS (
                      SELECT 1 FROM "CohortTypeFields" f2
                      WHERE f2."CohortTypeId" = t."CohortTypeId"
                        AND f2."Label" = v.label
                        AND f2."DeletedAt" IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "CohortTypeFields" f
                USING "CohortTypes" t
                WHERE f."CohortTypeId" = t."CohortTypeId"
                  AND t."Name" ILIKE '%final project%'
                  AND f."Label" IN ('Final project name', 'Supervisor', 'Word count');
                """);
        }
    }
}
