using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartnerNumber",
                table: "Partners",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill existing partners with a random number in the same
            // PA-YYYYMMDD-RAND6 shape new partners get. random() + PartnerId
            // in the hash makes the value per-row, so the unique index below
            // can be created safely.
            migrationBuilder.Sql("""
                UPDATE "Partners"
                SET "PartnerNumber" = 'PA-'
                    || to_char(now() at time zone 'utc', 'YYYYMMDD')
                    || '-'
                    || upper(substr(md5(random()::text || "PartnerId"::text), 1, 6))
                WHERE "PartnerNumber" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_PartnerNumber",
                table: "Partners",
                column: "PartnerNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partners_PartnerNumber",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PartnerNumber",
                table: "Partners");
        }
    }
}
