using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpMailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "MailSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "MailSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "MailSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpSecurity",
                table: "MailSettings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "MailSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "MailSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "MailSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "MailSettings");

            migrationBuilder.DropColumn(
                name: "SmtpSecurity",
                table: "MailSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "MailSettings");
        }
    }
}
