using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    PaymentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentInstallmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdditionalInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.PaymentRecordId);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_AdditionalInvoices_AdditionalInvoiceId",
                        column: x => x.AdditionalInvoiceId,
                        principalTable: "AdditionalInvoices",
                        principalColumn: "AdditionalInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_PaymentInstallments_PaymentInstallmentId",
                        column: x => x.PaymentInstallmentId,
                        principalTable: "PaymentInstallments",
                        principalColumn: "PaymentInstallmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_AdditionalInvoiceId",
                table: "PaymentRecords",
                column: "AdditionalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_PaymentInstallmentId",
                table: "PaymentRecords",
                column: "PaymentInstallmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRecords");
        }
    }
}
