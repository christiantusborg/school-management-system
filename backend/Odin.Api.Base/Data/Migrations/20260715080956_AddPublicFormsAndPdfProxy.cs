using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicFormsAndPdfProxy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicForms",
                columns: table => new
                {
                    PublicFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QuestionnaireTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    PriceAmountCents = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicForms", x => x.PublicFormId);
                    table.ForeignKey(
                        name: "FK_PublicForms_IntakeDocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalTable: "IntakeDocumentTemplates",
                        principalColumn: "DocumentTemplateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PublicForms_QuestionnaireTemplates_QuestionnaireTemplateId",
                        column: x => x.QuestionnaireTemplateId,
                        principalTable: "QuestionnaireTemplates",
                        principalColumn: "QuestionnaireTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicFormSubmissions",
                columns: table => new
                {
                    PublicFormSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnswersJson = table.Column<string>(type: "text", nullable: false),
                    QuestionnaireVersionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RespondentEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RespondentName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicFormSubmissions", x => x.PublicFormSubmissionId);
                    table.ForeignKey(
                        name: "FK_PublicFormSubmissions_PublicForms_PublicFormId",
                        column: x => x.PublicFormId,
                        principalTable: "PublicForms",
                        principalColumn: "PublicFormId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublicFormPayments",
                columns: table => new
                {
                    PublicFormPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublicFormSubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountCents = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicFormPayments", x => x.PublicFormPaymentId);
                    table.ForeignKey(
                        name: "FK_PublicFormPayments_PublicFormSubmissions_PublicFormSubmissi~",
                        column: x => x.PublicFormSubmissionId,
                        principalTable: "PublicFormSubmissions",
                        principalColumn: "PublicFormSubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicFormPayments_PublicFormSubmissionId",
                table: "PublicFormPayments",
                column: "PublicFormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicForms_DocumentTemplateId",
                table: "PublicForms",
                column: "DocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicForms_QuestionnaireTemplateId",
                table: "PublicForms",
                column: "QuestionnaireTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicForms_Slug",
                table: "PublicForms",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicFormSubmissions_PublicFormId",
                table: "PublicFormSubmissions",
                column: "PublicFormId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicFormPayments");

            migrationBuilder.DropTable(
                name: "PublicFormSubmissions");

            migrationBuilder.DropTable(
                name: "PublicForms");
        }
    }
}
