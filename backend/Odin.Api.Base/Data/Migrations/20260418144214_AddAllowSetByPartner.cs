using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowSetByPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowSetByPartner",
                table: "FinalProjectStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowSetByPartner",
                table: "EnrollmentStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 1,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 2,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 3,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 4,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 5,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 6,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 7,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 8,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 9,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 10,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "EnrollmentStatuses",
                keyColumn: "EnrollmentStatusId",
                keyValue: 11,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "FinalProjectStatuses",
                keyColumn: "FinalProjectStatusId",
                keyValue: 1,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "FinalProjectStatuses",
                keyColumn: "FinalProjectStatusId",
                keyValue: 2,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "FinalProjectStatuses",
                keyColumn: "FinalProjectStatusId",
                keyValue: 3,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "FinalProjectStatuses",
                keyColumn: "FinalProjectStatusId",
                keyValue: 4,
                column: "AllowSetByPartner",
                value: false);

            migrationBuilder.UpdateData(
                table: "FinalProjectStatuses",
                keyColumn: "FinalProjectStatusId",
                keyValue: 5,
                column: "AllowSetByPartner",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowSetByPartner",
                table: "FinalProjectStatuses");

            migrationBuilder.DropColumn(
                name: "AllowSetByPartner",
                table: "EnrollmentStatuses");
        }
    }
}
