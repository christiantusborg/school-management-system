using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerDatasheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerDatasheetDefinitions",
                columns: table => new
                {
                    PartnerDatasheetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheetDefinitions", x => x.PartnerDatasheetDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDatasheetFields",
                columns: table => new
                {
                    PartnerDatasheetFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OptionsText = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheetFields", x => x.PartnerDatasheetFieldId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDatasheetRows",
                columns: table => new
                {
                    PartnerDatasheetRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheetRows", x => x.PartnerDatasheetRowId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDatasheets",
                columns: table => new
                {
                    PartnerDatasheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheets", x => x.PartnerDatasheetId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDatasheetSections",
                columns: table => new
                {
                    PartnerDatasheetSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheetSections", x => x.PartnerDatasheetSectionId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDatasheetValues",
                columns: table => new
                {
                    PartnerDatasheetValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDatasheetFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDatasheetValues", x => x.PartnerDatasheetValueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheetFields_PartnerDatasheetSectionId",
                table: "PartnerDatasheetFields",
                column: "PartnerDatasheetSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheetRows_PartnerDatasheetId",
                table: "PartnerDatasheetRows",
                column: "PartnerDatasheetId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheets_PartnerDatasheetDefinitionId",
                table: "PartnerDatasheets",
                column: "PartnerDatasheetDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheets_PartnerId",
                table: "PartnerDatasheets",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheetSections_PartnerDatasheetDefinitionId",
                table: "PartnerDatasheetSections",
                column: "PartnerDatasheetDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheetValues_PartnerDatasheetRowId_PartnerDatashe~",
                table: "PartnerDatasheetValues",
                columns: new[] { "PartnerDatasheetRowId", "PartnerDatasheetFieldId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerDatasheetDefinitions");

            migrationBuilder.DropTable(
                name: "PartnerDatasheetFields");

            migrationBuilder.DropTable(
                name: "PartnerDatasheetRows");

            migrationBuilder.DropTable(
                name: "PartnerDatasheets");

            migrationBuilder.DropTable(
                name: "PartnerDatasheetSections");

            migrationBuilder.DropTable(
                name: "PartnerDatasheetValues");
        }
    }
}
