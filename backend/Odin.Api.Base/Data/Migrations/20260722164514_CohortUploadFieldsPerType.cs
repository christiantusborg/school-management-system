using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class CohortUploadFieldsPerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CohortTypeId",
                table: "CohortUploadFields",
                type: "uuid",
                nullable: true);

            // Existing global upload fields belong to the first (Standard)
            // cohort type — the type your existing cohorts were adopted onto.
            migrationBuilder.Sql("""
                UPDATE "CohortUploadFields" SET "CohortTypeId" =
                    (SELECT "CohortTypeId" FROM "CohortTypes"
                     WHERE "DeletedAt" IS NULL ORDER BY "SortOrder", "Name" LIMIT 1)
                WHERE "CohortTypeId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CohortTypeId",
                table: "CohortUploadFields");
        }
    }
}
