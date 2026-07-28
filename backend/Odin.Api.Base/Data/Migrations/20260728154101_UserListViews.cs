using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserListViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserListViewColumns",
                columns: table => new
                {
                    UserListViewColumnId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserListViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColumnKey = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListViewColumns", x => x.UserListViewColumnId);
                });

            migrationBuilder.CreateTable(
                name: "UserListViews",
                columns: table => new
                {
                    UserListViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Page = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListViews", x => x.UserListViewId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserListViewColumns");

            migrationBuilder.DropTable(
                name: "UserListViews");
        }
    }
}
