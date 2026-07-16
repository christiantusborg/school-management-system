using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPlanPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountDetails",
                table: "EnrollmentPaymentPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardPaymentLink",
                table: "EnrollmentPaymentPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PayByBankEnabled",
                table: "EnrollmentPaymentPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayByCardEnabled",
                table: "EnrollmentPaymentPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountDetails",
                table: "EnrollmentPaymentPlans");

            migrationBuilder.DropColumn(
                name: "CardPaymentLink",
                table: "EnrollmentPaymentPlans");

            migrationBuilder.DropColumn(
                name: "PayByBankEnabled",
                table: "EnrollmentPaymentPlans");

            migrationBuilder.DropColumn(
                name: "PayByCardEnabled",
                table: "EnrollmentPaymentPlans");
        }
    }
}
