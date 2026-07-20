using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleCohorts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CohortUploadFields",
                columns: table => new
                {
                    CohortUploadFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    AllowMultiple = table.Column<bool>(type: "boolean", nullable: false),
                    IsGradingSheet = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortUploadFields", x => x.CohortUploadFieldId);
                });

            migrationBuilder.CreateTable(
                name: "CohortUploadFiles",
                columns: table => new
                {
                    CohortUploadFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortUploadFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortUploadFiles", x => x.CohortUploadFileId);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCohorts",
                columns: table => new
                {
                    ModuleCohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    CohortNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GradingSheetDueOverride = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GradingSheetUploadedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DocQaChecked = table.Column<bool>(type: "boolean", nullable: false),
                    DocQaDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    GradeQaChecked = table.Column<bool>(type: "boolean", nullable: false),
                    GradeQaDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Reminder2WeeksSent = table.Column<bool>(type: "boolean", nullable: false),
                    Reminder1WeekSent = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderOverdueSent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCohorts", x => x.ModuleCohortId);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCohortSettings",
                columns: table => new
                {
                    ModuleCohortSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortNumberPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCohortSettings", x => x.ModuleCohortSettingsId);
                });

            migrationBuilder.CreateTable(
                name: "ModuleCohortStudents",
                columns: table => new
                {
                    ModuleCohortStudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleCohortStudents", x => x.ModuleCohortStudentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CohortUploadFiles_ModuleCohortId",
                table: "CohortUploadFiles",
                column: "ModuleCohortId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohorts_PartnerId",
                table: "ModuleCohorts",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohorts_SubjectId",
                table: "ModuleCohorts",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohorts_TeacherId",
                table: "ModuleCohorts",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohortStudents_ModuleCohortId",
                table: "ModuleCohortStudents",
                column: "ModuleCohortId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleCohortStudents_StudentEnrollmentId",
                table: "ModuleCohortStudents",
                column: "StudentEnrollmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CohortUploadFields");

            migrationBuilder.DropTable(
                name: "CohortUploadFiles");

            migrationBuilder.DropTable(
                name: "ModuleCohorts");

            migrationBuilder.DropTable(
                name: "ModuleCohortSettings");

            migrationBuilder.DropTable(
                name: "ModuleCohortStudents");
        }
    }
}
