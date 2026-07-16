using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolMailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolMailSettings",
                columns: table => new
                {
                    SchoolMailSettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    GmailServiceAccountJson = table.Column<string>(type: "text", nullable: true),
                    GmailImpersonatedUser = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmtpHost = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: true),
                    SmtpUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmtpPassword = table.Column<string>(type: "text", nullable: true),
                    SmtpSecurity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FromEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FromName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolMailSettings", x => x.SchoolMailSettingsId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolMailSettings_SchoolId",
                table: "SchoolMailSettings",
                column: "SchoolId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolMailSettings");
        }
    }
}
