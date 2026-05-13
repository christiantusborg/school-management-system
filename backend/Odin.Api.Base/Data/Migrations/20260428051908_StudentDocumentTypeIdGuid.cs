using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentDocumentTypeIdGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PG can't cast int -> uuid in place. Existing rows had
            // DocumentTypeId = 0 (placeholder from the previous workaround) so
            // the only sensible mapping is "none" — and the new column gets a
            // FK to DocumentTypes (Guid PK). To satisfy the FK on existing
            // rows we'd need to know the right Guid per row; we don't (the
            // workaround dropped that information). So we drop the test rows.
            // Test/wizard uploads only — production hasn't started.
            migrationBuilder.Sql(@"DELETE FROM ""StudentDocuments"";");
            migrationBuilder.DropColumn(name: "DocumentTypeId", table: "StudentDocuments");
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentTypeId",
                table: "StudentDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_DocumentTypeId",
                table: "StudentDocuments",
                column: "DocumentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDocuments_DocumentTypes_DocumentTypeId",
                table: "StudentDocuments",
                column: "DocumentTypeId",
                principalTable: "DocumentTypes",
                principalColumn: "DocumentTypeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentDocuments_DocumentTypes_DocumentTypeId",
                table: "StudentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StudentDocuments_DocumentTypeId",
                table: "StudentDocuments");

            migrationBuilder.DropColumn(name: "DocumentTypeId", table: "StudentDocuments");
            migrationBuilder.AddColumn<int>(
                name: "DocumentTypeId",
                table: "StudentDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
