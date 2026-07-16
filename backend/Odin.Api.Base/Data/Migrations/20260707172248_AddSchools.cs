using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Programmes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_SchoolId",
                table: "Programmes",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Schools_SchoolId",
                table: "Programmes",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "SchoolId",
                onDelete: ReferentialAction.Restrict);

            // Seed the default IBSS school and assign every existing programme
            // (core and partner-owned) to it, so no programme is left without a
            // school. Admins can add more schools and reassign afterwards.
            migrationBuilder.Sql(@"
                INSERT INTO ""Schools"" (""SchoolId"", ""Name"", ""Description"", ""DeletedAt"")
                VALUES ('11111111-1111-1111-1111-111111111111',
                        'IBSS',
                        'International Business School of Scandinavia', NULL);
                UPDATE ""Programmes"" SET ""SchoolId"" = '11111111-1111-1111-1111-111111111111'
                WHERE ""SchoolId"" IS NULL AND ""DeletedAt"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Schools_SchoolId",
                table: "Programmes");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_SchoolId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Programmes");
        }
    }
}
