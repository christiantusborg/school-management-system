using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovePaymentMethodsToInstallment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "BankAccountDetails",
                table: "PaymentInstallments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardPaymentLink",
                table: "PaymentInstallments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PayByBankEnabled",
                table: "PaymentInstallments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PayByCardEnabled",
                table: "PaymentInstallments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountDetails",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "CardPaymentLink",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "PayByBankEnabled",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "PayByCardEnabled",
                table: "PaymentInstallments");

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
    }
}
