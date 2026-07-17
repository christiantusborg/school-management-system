using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleEndDatesAndDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "EnrollmentModuleStarts",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndOffsetDays",
                table: "EnrollmentModuleStarts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EndUseOffset",
                table: "EnrollmentModuleStarts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultEndOffsetDays",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DefaultStartOffsetDays",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "EnrollmentModuleStarts");

            migrationBuilder.DropColumn(
                name: "EndOffsetDays",
                table: "EnrollmentModuleStarts");

            migrationBuilder.DropColumn(
                name: "EndUseOffset",
                table: "EnrollmentModuleStarts");
        }
    }
}
