using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLogNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentLogNotes",
                columns: table => new
                {
                    StudentLogNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthorUserId = table.Column<string>(type: "text", nullable: true),
                    VisibleToPartner = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleToStudent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLogNotes", x => x.StudentLogNoteId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLogNotes_StudentEnrollmentId",
                table: "StudentLogNotes",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLogNotes_StudentId",
                table: "StudentLogNotes",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentLogNotes");
        }
    }
}
