using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2AccessLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "RolePermissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoleStatusAccesses",
                columns: table => new
                {
                    RoleStatusAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StatusId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleStatusAccesses", x => x.RoleStatusAccessId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleStatusAccesses_RoleName_StatusId",
                table: "RoleStatusAccesses",
                columns: new[] { "RoleName", "StatusId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleStatusAccesses");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "RolePermissions");
        }
    }
}
