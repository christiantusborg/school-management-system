using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class Rubrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RubricTemplateId",
                table: "Subjects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RubricRows",
                columns: table => new
                {
                    RubricRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Criteria = table.Column<string>(type: "text", nullable: false),
                    MaxPercent = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricRows", x => x.RubricRowId);
                });

            migrationBuilder.CreateTable(
                name: "RubricTemplates",
                columns: table => new
                {
                    RubricTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerSubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricTemplates", x => x.RubricTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "SubjectGradeRubricScores",
                columns: table => new
                {
                    SubjectGradeRubricScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectGradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGradeRubricScores", x => x.SubjectGradeRubricScoreId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RubricRows");

            migrationBuilder.DropTable(
                name: "RubricTemplates");

            migrationBuilder.DropTable(
                name: "SubjectGradeRubricScores");

            migrationBuilder.DropColumn(
                name: "RubricTemplateId",
                table: "Subjects");
        }
    }
}
