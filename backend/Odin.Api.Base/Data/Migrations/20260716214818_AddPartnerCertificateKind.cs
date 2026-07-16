using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerCertificateKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartnerCertificates_PartnerId_SchoolId",
                table: "PartnerCertificates");

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "PartnerCertificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PartnerCertificates_PartnerId_SchoolId_Kind",
                table: "PartnerCertificates",
                columns: new[] { "PartnerId", "SchoolId", "Kind" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartnerCertificates_PartnerId_SchoolId_Kind",
                table: "PartnerCertificates");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "PartnerCertificates");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerCertificates_PartnerId_SchoolId",
                table: "PartnerCertificates",
                columns: new[] { "PartnerId", "SchoolId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}
