using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentsAndTeacherRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTeacher",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AssignmentUploads",
                columns: table => new
                {
                    AssignmentUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedByRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UploadedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UploadedByUserId = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentUploads", x => x.AssignmentUploadId);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentComments",
                columns: table => new
                {
                    AssignmentCommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AuthorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AuthorUserId = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentComments", x => x.AssignmentCommentId);
                    table.ForeignKey(
                        name: "FK_AssignmentComments_AssignmentUploads_AssignmentUploadId",
                        column: x => x.AssignmentUploadId,
                        principalTable: "AssignmentUploads",
                        principalColumn: "AssignmentUploadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentComments_AssignmentUploadId",
                table: "AssignmentComments",
                column: "AssignmentUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentUploads_StudentEnrollmentId_SubjectId",
                table: "AssignmentUploads",
                columns: new[] { "StudentEnrollmentId", "SubjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentComments");

            migrationBuilder.DropTable(
                name: "AssignmentUploads");

            migrationBuilder.DropColumn(
                name: "IsTeacher",
                table: "AspNetUsers");
        }
    }
}
