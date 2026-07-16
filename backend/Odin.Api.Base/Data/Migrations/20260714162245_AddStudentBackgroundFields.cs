using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentBackgroundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentEmploymentIndustryId",
                table: "Students",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPositionFunctionId",
                table: "Students",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabilityDisclosure",
                table: "Students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabilitySupportNeeds",
                table: "Students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Students",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalaryAmount",
                table: "Students",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MonthlySalaryCurrencyId",
                table: "Students",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmploymentIndustries",
                columns: table => new
                {
                    EmploymentIndustryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentIndustries", x => x.EmploymentIndustryId);
                });

            migrationBuilder.CreateTable(
                name: "PositionFunctions",
                columns: table => new
                {
                    PositionFunctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionFunctions", x => x.PositionFunctionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_CurrentEmploymentIndustryId",
                table: "Students",
                column: "CurrentEmploymentIndustryId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CurrentPositionFunctionId",
                table: "Students",
                column: "CurrentPositionFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_MonthlySalaryCurrencyId",
                table: "Students",
                column: "MonthlySalaryCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Currencies_MonthlySalaryCurrencyId",
                table: "Students",
                column: "MonthlySalaryCurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_EmploymentIndustries_CurrentEmploymentIndustryId",
                table: "Students",
                column: "CurrentEmploymentIndustryId",
                principalTable: "EmploymentIndustries",
                principalColumn: "EmploymentIndustryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_PositionFunctions_CurrentPositionFunctionId",
                table: "Students",
                column: "CurrentPositionFunctionId",
                principalTable: "PositionFunctions",
                principalColumn: "PositionFunctionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Currencies_MonthlySalaryCurrencyId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_EmploymentIndustries_CurrentEmploymentIndustryId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_PositionFunctions_CurrentPositionFunctionId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "EmploymentIndustries");

            migrationBuilder.DropTable(
                name: "PositionFunctions");

            migrationBuilder.DropIndex(
                name: "IX_Students_CurrentEmploymentIndustryId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CurrentPositionFunctionId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_MonthlySalaryCurrencyId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CurrentEmploymentIndustryId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CurrentPositionFunctionId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DisabilityDisclosure",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DisabilitySupportNeeds",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MonthlySalaryAmount",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MonthlySalaryCurrencyId",
                table: "Students");
        }
    }
}
