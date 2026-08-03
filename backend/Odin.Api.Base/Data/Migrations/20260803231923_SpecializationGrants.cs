using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpecializationGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpecializationPartners",
                columns: table => new
                {
                    SpecializationPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisabledByPartner = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializationPartners", x => x.SpecializationPartnerId);
                    table.ForeignKey(
                        name: "FK_SpecializationPartners_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecializationPartners_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalTable: "Specializations",
                        principalColumn: "SpecializationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecializationPartners_PartnerId",
                table: "SpecializationPartners",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecializationPartners_SpecializationId_PartnerId",
                table: "SpecializationPartners",
                columns: new[] { "SpecializationId", "PartnerId" },
                unique: true);

            // Core-programme access becomes per-specialization: every active
            // programme-level grant fans out to one row per (non-deleted)
            // specialization of that programme, so partners keep exactly the
            // access they had. Partner opt-outs start clear.
            migrationBuilder.Sql("""
                INSERT INTO "SpecializationPartners"
                    ("SpecializationPartnerId", "SpecializationId", "PartnerId", "DisabledByPartner", "GrantedAt")
                SELECT gen_random_uuid(), s."SpecializationId", pp."PartnerId", false, now()
                FROM "ProgrammePartners" pp
                JOIN "Specializations" s ON s."ProgrammeId" = pp."ProgrammeId"
                    AND s."DeletedAt" IS NULL
                WHERE pp."IsActive" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecializationPartners");
        }
    }
}
