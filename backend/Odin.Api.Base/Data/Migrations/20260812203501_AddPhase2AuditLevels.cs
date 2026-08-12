using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2AuditLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NewLevel",
                table: "PermissionAuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OldLevel",
                table: "PermissionAuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                table: "PermissionAuditLogs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewLevel",
                table: "PermissionAuditLogs");

            migrationBuilder.DropColumn(
                name: "OldLevel",
                table: "PermissionAuditLogs");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "PermissionAuditLogs");
        }
    }
}
