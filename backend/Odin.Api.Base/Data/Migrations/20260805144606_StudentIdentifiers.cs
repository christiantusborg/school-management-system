using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentIdentifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentIdentifiers",
                columns: table => new
                {
                    StudentIdentifierId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentIdentifiers", x => x.StudentIdentifierId);
                    table.ForeignKey(
                        name: "FK_StudentIdentifiers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentIdentifiers_StudentId",
                table: "StudentIdentifiers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentIdentifiers_Value",
                table: "StudentIdentifiers",
                column: "Value",
                unique: true);

            // Every student's current StudentNumber becomes their PRIMARY
            // identifier row; extra IDs (aliases) are added on top later.
            migrationBuilder.Sql("""
                INSERT INTO "StudentIdentifiers" ("StudentIdentifierId", "StudentId", "Value", "Label", "IsPrimary", "CreatedAt")
                SELECT gen_random_uuid(), s."StudentId", s."StudentNumber", NULL, true, now()
                FROM "Students" s
                WHERE s."StudentNumber" IS NOT NULL AND s."StudentNumber" <> '';
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentIdentifiers");
        }
    }
}
