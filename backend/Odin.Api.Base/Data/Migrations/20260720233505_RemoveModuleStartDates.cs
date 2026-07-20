using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveModuleStartDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentModuleStarts");

            migrationBuilder.DropColumn(
                name: "DefaultEndOffsetDays",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DefaultStartOffsetDays",
                table: "Subjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultEndOffsetDays",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultStartOffsetDays",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnrollmentModuleStarts",
                columns: table => new
                {
                    EnrollmentModuleStartId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    EndUseOffset = table.Column<bool>(type: "boolean", nullable: false),
                    OffsetDays = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UseOffset = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentModuleStarts", x => x.EnrollmentModuleStartId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentModuleStarts_StudentEnrollmentId_SubjectId",
                table: "EnrollmentModuleStarts",
                columns: new[] { "StudentEnrollmentId", "SubjectId" },
                unique: true);
        }
    }
}
