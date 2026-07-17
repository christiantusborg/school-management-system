using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatasheetGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "PartnerDatasheets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentPartnerDatasheetId",
                table: "PartnerDatasheets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartnerCanAddItems",
                table: "PartnerDatasheets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDatasheets_ParentPartnerDatasheetId",
                table: "PartnerDatasheets",
                column: "ParentPartnerDatasheetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartnerDatasheets_ParentPartnerDatasheetId",
                table: "PartnerDatasheets");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PartnerDatasheets");

            migrationBuilder.DropColumn(
                name: "ParentPartnerDatasheetId",
                table: "PartnerDatasheets");

            migrationBuilder.DropColumn(
                name: "PartnerCanAddItems",
                table: "PartnerDatasheets");
        }
    }
}
