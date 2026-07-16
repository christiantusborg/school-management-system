using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.CurrencyId);
                });

            // Seed the default currency list (incl. RM and CHF). Admins can add,
            // rename, reorder, or soft-delete these in System Config → Currencies.
            migrationBuilder.Sql(@"
                INSERT INTO ""Currencies"" (""CurrencyId"", ""Code"", ""Name"", ""DisplayOrder"", ""DeletedAt"") VALUES
                  (gen_random_uuid(), 'USD', 'US Dollar',          1, NULL),
                  (gen_random_uuid(), 'EUR', 'Euro',               2, NULL),
                  (gen_random_uuid(), 'GBP', 'British Pound',      3, NULL),
                  (gen_random_uuid(), 'DKK', 'Danish Krone',       4, NULL),
                  (gen_random_uuid(), 'CHF', 'Swiss Franc',        5, NULL),
                  (gen_random_uuid(), 'RM',  'Malaysian Ringgit',  6, NULL),
                  (gen_random_uuid(), 'VND', 'Vietnamese Dong',    7, NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Currencies");
        }
    }
}
