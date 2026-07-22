using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCohortTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CohortTypeId",
                table: "ModuleCohorts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CohortFieldValues",
                columns: table => new
                {
                    CohortFieldValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleCohortId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortTypeFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortFieldValues", x => x.CohortFieldValueId);
                });

            migrationBuilder.CreateTable(
                name: "CohortTypeFields",
                columns: table => new
                {
                    CohortTypeFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CohortTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OptionsText = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortTypeFields", x => x.CohortTypeFieldId);
                });

            migrationBuilder.CreateTable(
                name: "CohortTypes",
                columns: table => new
                {
                    CohortTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CohortTypes", x => x.CohortTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CohortFieldValues_ModuleCohortId_CohortTypeFieldId",
                table: "CohortFieldValues",
                columns: new[] { "ModuleCohortId", "CohortTypeFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CohortTypeFields_CohortTypeId",
                table: "CohortTypeFields",
                column: "CohortTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CohortFieldValues");

            migrationBuilder.DropTable(
                name: "CohortTypeFields");

            migrationBuilder.DropTable(
                name: "CohortTypes");

            migrationBuilder.DropColumn(
                name: "CohortTypeId",
                table: "ModuleCohorts");
        }
    }
}
