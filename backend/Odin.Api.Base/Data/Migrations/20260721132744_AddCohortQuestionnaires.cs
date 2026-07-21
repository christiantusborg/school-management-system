using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCohortQuestionnaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CohortQuestionnaireCompletions",
                columns: table => new
                {
                    CohortQuestionnaireCompletionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortQuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortQuestionnaireCompletions", x => x.CohortQuestionnaireCompletionId);
                });

            migrationBuilder.CreateTable(
                name: "CohortQuestionnaireResponses",
                columns: table => new
                {
                    CohortQuestionnaireResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortQuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    QuestionnaireVersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortQuestionnaireResponses", x => x.CohortQuestionnaireResponseId);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCohortQuestionnaires",
                columns: table => new
                {
                    ModuleCohortQuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCohortQuestionnaires", x => x.ModuleCohortQuestionnaireId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CohortQuestionnaireCompletions_ModuleCohortQuestionnaireId_~",
                table: "CohortQuestionnaireCompletions",
                columns: new[] { "ModuleCohortQuestionnaireId", "StudentEnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CohortQuestionnaireCompletions_StudentEnrollmentId",
                table: "CohortQuestionnaireCompletions",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CohortQuestionnaireResponses_ModuleCohortQuestionnaireId",
                table: "CohortQuestionnaireResponses",
                column: "ModuleCohortQuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohortQuestionnaires_ModuleCohortId",
                table: "ModuleCohortQuestionnaires",
                column: "ModuleCohortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CohortQuestionnaireCompletions");

            migrationBuilder.DropTable(
                name: "CohortQuestionnaireResponses");

            migrationBuilder.DropTable(
                name: "ModuleCohortQuestionnaires");
        }
    }
}
