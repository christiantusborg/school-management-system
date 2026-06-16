using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LetterEmailTemplates",
                columns: table => new
                {
                    LetterEmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LetterType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEmailEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    CcRecipientsJson = table.Column<string>(type: "jsonb", nullable: true),
                    BccRecipientsJson = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterEmailTemplates", x => x.LetterEmailTemplateId);
                    table.ForeignKey(
                        name: "FK_LetterEmailTemplates_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_LetterType",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LetterEmailTemplates");
        }
    }
}
