using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeInstancesAndResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntakeInstances",
                columns: table => new
                {
                    IntakeInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    QuestionnaireTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    InlineDefinitionJson = table.Column<string>(type: "text", nullable: true),
                    OutputProfileJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeInstances", x => x.IntakeInstanceId);
                    table.ForeignKey(
                        name: "FK_IntakeInstances_QuestionnaireTemplates_QuestionnaireTemplat~",
                        column: x => x.QuestionnaireTemplateId,
                        principalTable: "QuestionnaireTemplates",
                        principalColumn: "QuestionnaireTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntakeResponses",
                columns: table => new
                {
                    IntakeResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeInstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LifecycleState = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    QuestionnaireVersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeResponses", x => x.IntakeResponseId);
                    table.ForeignKey(
                        name: "FK_IntakeResponses_IntakeInstances_IntakeInstanceId",
                        column: x => x.IntakeInstanceId,
                        principalTable: "IntakeInstances",
                        principalColumn: "IntakeInstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeInstances_Audience",
                table: "IntakeInstances",
                column: "Audience");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeInstances_QuestionnaireTemplateId",
                table: "IntakeInstances",
                column: "QuestionnaireTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeResponses_IntakeInstanceId",
                table: "IntakeResponses",
                column: "IntakeInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeResponses_PartnerId",
                table: "IntakeResponses",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeResponses_StudentId",
                table: "IntakeResponses",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeResponses");

            migrationBuilder.DropTable(
                name: "IntakeInstances");
        }
    }
}
