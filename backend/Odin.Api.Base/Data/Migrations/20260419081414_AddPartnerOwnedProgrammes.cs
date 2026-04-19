using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerOwnedProgrammes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClonedFromProgrammeId",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Programmes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerId",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Programmes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Programmes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_ClonedFromProgrammeId",
                table: "Programmes",
                column: "ClonedFromProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_PartnerId_Status",
                table: "Programmes",
                columns: new[] { "PartnerId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Partners_PartnerId",
                table: "Programmes",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Programmes_ClonedFromProgrammeId",
                table: "Programmes",
                column: "ClonedFromProgrammeId",
                principalTable: "Programmes",
                principalColumn: "ProgrammeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Partners_PartnerId",
                table: "Programmes");

            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Programmes_ClonedFromProgrammeId",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_ClonedFromProgrammeId",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_PartnerId_Status",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "ClonedFromProgrammeId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Programmes");
        }
    }
}
