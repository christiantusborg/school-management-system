using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceFeesWithAdditionalInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdditionalInvoices",
                columns: table => new
                {
                    AdditionalInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PayByCardEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CardPaymentLink = table.Column<string>(type: "text", nullable: true),
                    PayByBankEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BankAccountDetails = table.Column<string>(type: "text", nullable: true),
                    LinesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalInvoices", x => x.AdditionalInvoiceId);
                    table.ForeignKey(
                        name: "FK_AdditionalInvoices_EnrollmentPaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "EnrollmentPaymentPlans",
                        principalColumn: "PaymentPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalInvoices_PaymentPlanId",
                table: "AdditionalInvoices",
                column: "PaymentPlanId");

            // Preserve already-entered fees: each non-zero Registration /
            // Final-project fee becomes a one-line additional invoice, then
            // the two columns are dropped.
            migrationBuilder.Sql("""
                INSERT INTO "AdditionalInvoices"
                    ("AdditionalInvoiceId","PaymentPlanId","Sequence","IsPaid","PayByCardEnabled","PayByBankEnabled","LinesJson")
                SELECT gen_random_uuid(), p."PaymentPlanId", 1, FALSE, FALSE, FALSE,
                       '[{"text":"Registration fee","amount":' || p."RegistrationFee"::text || '}]'
                FROM "EnrollmentPaymentPlans" p
                WHERE p."RegistrationFee" > 0;

                INSERT INTO "AdditionalInvoices"
                    ("AdditionalInvoiceId","PaymentPlanId","Sequence","IsPaid","PayByCardEnabled","PayByBankEnabled","LinesJson")
                SELECT gen_random_uuid(), p."PaymentPlanId",
                       CASE WHEN p."RegistrationFee" > 0 THEN 2 ELSE 1 END,
                       FALSE, FALSE, FALSE,
                       '[{"text":"Final project fee","amount":' || p."FinalProjectFee"::text || '}]'
                FROM "EnrollmentPaymentPlans" p
                WHERE p."FinalProjectFee" > 0;
                """);

            migrationBuilder.DropColumn(
                name: "FinalProjectFee",
                table: "EnrollmentPaymentPlans");

            migrationBuilder.DropColumn(
                name: "RegistrationFee",
                table: "EnrollmentPaymentPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalInvoices");

            migrationBuilder.AddColumn<decimal>(
                name: "FinalProjectFee",
                table: "EnrollmentPaymentPlans",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RegistrationFee",
                table: "EnrollmentPaymentPlans",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
