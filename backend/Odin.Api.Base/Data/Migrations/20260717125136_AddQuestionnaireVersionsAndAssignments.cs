using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionnaireVersionsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentMode",
                table: "IntakeInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "IntakeAssignments",
                columns: table => new
                {
                    IntakeAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeAssignments", x => x.IntakeAssignmentId);
                    table.ForeignKey(
                        name: "FK_IntakeAssignments_IntakeInstances_IntakeInstanceId",
                        column: x => x.IntakeInstanceId,
                        principalTable: "IntakeInstances",
                        principalColumn: "IntakeInstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireTemplateVersions",
                columns: table => new
                {
                    QuestionnaireTemplateVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FrozenAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireTemplateVersions", x => x.QuestionnaireTemplateVersionId);
                    table.ForeignKey(
                        name: "FK_QuestionnaireTemplateVersions_QuestionnaireTemplates_Questi~",
                        column: x => x.QuestionnaireTemplateId,
                        principalTable: "QuestionnaireTemplates",
                        principalColumn: "QuestionnaireTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeAssignments_IntakeInstanceId",
                table: "IntakeAssignments",
                column: "IntakeInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireTemplateVersions_QuestionnaireTemplateId_Defin~",
                table: "QuestionnaireTemplateVersions",
                columns: new[] { "QuestionnaireTemplateId", "DefinitionHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeAssignments");

            migrationBuilder.DropTable(
                name: "QuestionnaireTemplateVersions");

            migrationBuilder.DropColumn(
                name: "AssignmentMode",
                table: "IntakeInstances");
        }
    }
}
