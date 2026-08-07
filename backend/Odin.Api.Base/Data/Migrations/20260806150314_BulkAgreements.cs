using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class BulkAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkAgreements",
                columns: table => new
                {
                    BulkAgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TargetStudents = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkAgreements", x => x.BulkAgreementId);
                    table.ForeignKey(
                        name: "FK_BulkAgreements_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkAgreementSpecializations",
                columns: table => new
                {
                    BulkAgreementSpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BulkAgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkAgreementSpecializations", x => x.BulkAgreementSpecializationId);
                    table.ForeignKey(
                        name: "FK_BulkAgreementSpecializations_BulkAgreements_BulkAgreementId",
                        column: x => x.BulkAgreementId,
                        principalTable: "BulkAgreements",
                        principalColumn: "BulkAgreementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkAgreements_PartnerId",
                table: "BulkAgreements",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkAgreementSpecializations_BulkAgreementId_Specialization~",
                table: "BulkAgreementSpecializations",
                columns: new[] { "BulkAgreementId", "SpecializationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkAgreementSpecializations");

            migrationBuilder.DropTable(
                name: "BulkAgreements");
        }
    }
}
