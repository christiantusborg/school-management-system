using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyTeachers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacultyProfileFields",
                columns: table => new
                {
                    FacultyProfileFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OptionsText = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    PartnerCanEdit = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyProfileFields", x => x.FacultyProfileFieldId);
                });

            migrationBuilder.CreateTable(
                name: "FacultyProfileSections",
                columns: table => new
                {
                    FacultyProfileSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyProfileSections", x => x.FacultyProfileSectionId);
                });

            migrationBuilder.CreateTable(
                name: "TeacherProfileRows",
                columns: table => new
                {
                    TeacherProfileRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherProfileRows", x => x.TeacherProfileRowId);
                });

            migrationBuilder.CreateTable(
                name: "TeacherProfileValues",
                columns: table => new
                {
                    TeacherProfileValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherProfileRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherProfileValues", x => x.TeacherProfileValueId);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.TeacherId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacultyProfileFields_FacultyProfileSectionId",
                table: "FacultyProfileFields",
                column: "FacultyProfileSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfileRows_TeacherId",
                table: "TeacherProfileRows",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfileValues_TeacherProfileRowId_FacultyProfileFiel~",
                table: "TeacherProfileValues",
                columns: new[] { "TeacherProfileRowId", "FacultyProfileFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_PartnerId",
                table: "Teachers",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_UserId",
                table: "Teachers",
                column: "UserId");

            // The Faculties feature previously piggybacked on the datasheet
            // tables (definition Scope = 'faculty'). Move that structure and
            // every teacher profile into the dedicated tables (ids preserved),
            // then retire the faculty rows on the datasheet side.
            migrationBuilder.Sql(@"
                INSERT INTO ""FacultyProfileSections"" (""FacultyProfileSectionId"",""Title"",""Kind"",""SortOrder"",""DeletedAt"")
                SELECT s.""PartnerDatasheetSectionId"", s.""Title"", s.""Kind"", s.""SortOrder"", s.""DeletedAt""
                FROM ""PartnerDatasheetSections"" s
                JOIN ""PartnerDatasheetDefinitions"" d ON d.""PartnerDatasheetDefinitionId"" = s.""PartnerDatasheetDefinitionId""
                WHERE d.""Scope"" = 'faculty' AND d.""DeletedAt"" IS NULL;

                INSERT INTO ""FacultyProfileFields"" (""FacultyProfileFieldId"",""FacultyProfileSectionId"",""Label"",""Type"",""OptionsText"",""IsRequired"",""PartnerCanEdit"",""SortOrder"",""DeletedAt"")
                SELECT f.""PartnerDatasheetFieldId"", f.""PartnerDatasheetSectionId"", f.""Label"", f.""Type"", f.""OptionsText"", f.""IsRequired"", f.""PartnerCanEdit"", f.""SortOrder"", f.""DeletedAt""
                FROM ""PartnerDatasheetFields"" f
                JOIN ""PartnerDatasheetSections"" s ON s.""PartnerDatasheetSectionId"" = f.""PartnerDatasheetSectionId""
                JOIN ""PartnerDatasheetDefinitions"" d ON d.""PartnerDatasheetDefinitionId"" = s.""PartnerDatasheetDefinitionId""
                WHERE d.""Scope"" = 'faculty' AND d.""DeletedAt"" IS NULL;

                INSERT INTO ""Teachers"" (""TeacherId"",""PartnerId"",""UserId"",""DisplayName"",""CreatedAt"",""UpdatedAt"",""DeletedAt"")
                SELECT ps.""PartnerDatasheetId"", ps.""PartnerId"", ps.""TeacherUserId"",
                       COALESCE(NULLIF(ps.""Title"",''),'Teacher'), ps.""CreatedAt"", ps.""UpdatedAt"", ps.""DeletedAt""
                FROM ""PartnerDatasheets"" ps
                JOIN ""PartnerDatasheetDefinitions"" d ON d.""PartnerDatasheetDefinitionId"" = ps.""PartnerDatasheetDefinitionId""
                WHERE d.""Scope"" = 'faculty';

                INSERT INTO ""TeacherProfileRows"" (""TeacherProfileRowId"",""TeacherId"",""FacultyProfileSectionId"",""SortOrder"",""DeletedAt"")
                SELECT r.""PartnerDatasheetRowId"", r.""PartnerDatasheetId"", r.""PartnerDatasheetSectionId"", r.""SortOrder"", r.""DeletedAt""
                FROM ""PartnerDatasheetRows"" r
                JOIN ""PartnerDatasheets"" ps ON ps.""PartnerDatasheetId"" = r.""PartnerDatasheetId""
                JOIN ""PartnerDatasheetDefinitions"" d ON d.""PartnerDatasheetDefinitionId"" = ps.""PartnerDatasheetDefinitionId""
                WHERE d.""Scope"" = 'faculty';

                INSERT INTO ""TeacherProfileValues"" (""TeacherProfileValueId"",""TeacherProfileRowId"",""FacultyProfileFieldId"",""Value"",""FileName"",""UpdatedAt"")
                SELECT v.""PartnerDatasheetValueId"", v.""PartnerDatasheetRowId"", v.""PartnerDatasheetFieldId"", v.""Value"", v.""FileName"", v.""UpdatedAt""
                FROM ""PartnerDatasheetValues"" v
                JOIN ""PartnerDatasheetRows"" r ON r.""PartnerDatasheetRowId"" = v.""PartnerDatasheetRowId""
                JOIN ""PartnerDatasheets"" ps ON ps.""PartnerDatasheetId"" = r.""PartnerDatasheetId""
                JOIN ""PartnerDatasheetDefinitions"" d ON d.""PartnerDatasheetDefinitionId"" = ps.""PartnerDatasheetDefinitionId""
                WHERE d.""Scope"" = 'faculty';

                UPDATE ""PartnerDatasheets"" ps SET ""DeletedAt"" = (now() at time zone 'utc')
                FROM ""PartnerDatasheetDefinitions"" d
                WHERE d.""PartnerDatasheetDefinitionId"" = ps.""PartnerDatasheetDefinitionId""
                  AND d.""Scope"" = 'faculty' AND ps.""DeletedAt"" IS NULL;

                UPDATE ""PartnerDatasheetDefinitions"" SET ""DeletedAt"" = (now() at time zone 'utc')
                WHERE ""Scope"" = 'faculty' AND ""DeletedAt"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacultyProfileFields");

            migrationBuilder.DropTable(
                name: "FacultyProfileSections");

            migrationBuilder.DropTable(
                name: "TeacherProfileRows");

            migrationBuilder.DropTable(
                name: "TeacherProfileValues");

            migrationBuilder.DropTable(
                name: "Teachers");
        }
    }
}
