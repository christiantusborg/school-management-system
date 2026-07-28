using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class CombinedInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CombinedInvoiceLines",
                columns: table => new
                {
                    CombinedInvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CombinedInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentInstallmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdditionalInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentName = table.Column<string>(type: "text", nullable: false),
                    StudentNumber = table.Column<string>(type: "text", nullable: false),
                    ProgrammeCode = table.Column<string>(type: "text", nullable: false),
                    ItemLabel = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedInvoiceLines", x => x.CombinedInvoiceLineId);
                });

            migrationBuilder.CreateTable(
                name: "CombinedInvoices",
                columns: table => new
                {
                    CombinedInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PaidByUserId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombinedInvoices", x => x.CombinedInvoiceId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CombinedInvoiceLines");

            migrationBuilder.DropTable(
                name: "CombinedInvoices");
        }
    }
}
