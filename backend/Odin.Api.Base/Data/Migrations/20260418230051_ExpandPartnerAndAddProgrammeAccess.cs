using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPartnerAndAddProgrammeAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonEmail",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonPhone",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonTitle",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEnd",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStart",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateRegion",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Partners",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartnerProgrammeAccesses",
                columns: table => new
                {
                    PartnerProgrammeAccessId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MajorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    DisabledByPartner = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerProgrammeAccesses", x => x.PartnerProgrammeAccessId);
                    table.ForeignKey(
                        name: "FK_PartnerProgrammeAccesses_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerProgrammeAccesses_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerProgrammeAccesses_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProgrammeAccesses_MajorId",
                table: "PartnerProgrammeAccesses",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProgrammeAccesses_PartnerId",
                table: "PartnerProgrammeAccesses",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProgrammeAccesses_PartnerId_MajorId_DeletedAt",
                table: "PartnerProgrammeAccesses",
                columns: new[] { "PartnerId", "MajorId", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerProgrammeAccesses_ProgrammeId",
                table: "PartnerProgrammeAccesses",
                column: "ProgrammeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerProgrammeAccesses");

            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPersonEmail",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPersonPhone",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPersonTitle",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContractEnd",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContractStart",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "StateRegion",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Partners");
        }
    }
}
