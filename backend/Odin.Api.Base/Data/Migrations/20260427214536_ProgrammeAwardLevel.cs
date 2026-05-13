using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProgrammeAwardLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AwardEducationLevelId",
                table: "Programmes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_AwardEducationLevelId",
                table: "Programmes",
                column: "AwardEducationLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_EducationLevels_AwardEducationLevelId",
                table: "Programmes",
                column: "AwardEducationLevelId",
                principalTable: "EducationLevels",
                principalColumn: "EducationLevelId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_EducationLevels_AwardEducationLevelId",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_AwardEducationLevelId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "AwardEducationLevelId",
                table: "Programmes");
        }
    }
}
