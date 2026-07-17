using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePartnerCertificatesWithPartnerDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerCertificates");

            migrationBuilder.CreateTable(
                name: "PartnerDocuments",
                columns: table => new
                {
                    PartnerDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerDocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldValuesJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDocuments", x => x.PartnerDocumentId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerDocumentTypes",
                columns: table => new
                {
                    PartnerDocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldsJson = table.Column<string>(type: "text", nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerDocumentTypes", x => x.PartnerDocumentTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDocuments_PartnerDocumentTypeId",
                table: "PartnerDocuments",
                column: "PartnerDocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerDocuments_PartnerId",
                table: "PartnerDocuments",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerDocuments");

            migrationBuilder.DropTable(
                name: "PartnerDocumentTypes");

            migrationBuilder.CreateTable(
                name: "PartnerCertificates",
                columns: table => new
                {
                    PartnerCertificateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateLayoutJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerCertificates", x => x.PartnerCertificateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerCertificates_PartnerId_SchoolId_Kind",
                table: "PartnerCertificates",
                columns: new[] { "PartnerId", "SchoolId", "Kind" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}
