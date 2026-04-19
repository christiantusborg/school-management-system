using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModeOfStudy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessLevelDefinitions");

            migrationBuilder.DropTable(
                name: "CaseFileLevelKeys");

            migrationBuilder.DropTable(
                name: "CaseKeyPairs");

            migrationBuilder.DropTable(
                name: "CaseLevelLabelOverrides");

            migrationBuilder.DropTable(
                name: "CaseTeamKeys");

            migrationBuilder.DropTable(
                name: "CaseTeamMemberOverrides");

            migrationBuilder.DropTable(
                name: "CaseTeamMemberships");

            migrationBuilder.DropTable(
                name: "CaseUserKeys");

            migrationBuilder.DropTable(
                name: "CaseUserMembers");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TeamRoles");

            migrationBuilder.DropTable(
                name: "TeamSymmetricKeys");

            migrationBuilder.DropTable(
                name: "TeamTypes");

            migrationBuilder.DropTable(
                name: "CaseFiles");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.DocumentTypeId);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentStatuses",
                columns: table => new
                {
                    EnrollmentStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentStatuses", x => x.EnrollmentStatusId);
                });

            migrationBuilder.CreateTable(
                name: "FinalProjectStatuses",
                columns: table => new
                {
                    FinalProjectStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalProjectStatuses", x => x.FinalProjectStatusId);
                });

            migrationBuilder.CreateTable(
                name: "ModesOfStudy",
                columns: table => new
                {
                    ModeOfStudyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModesOfStudy", x => x.ModeOfStudyId);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    PartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.PartnerId);
                    table.ForeignKey(
                        name: "FK_Partners_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pathways",
                columns: table => new
                {
                    PathwayId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pathways", x => x.PathwayId);
                });

            migrationBuilder.CreateTable(
                name: "Programmes",
                columns: table => new
                {
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmes", x => x.ProgrammeId);
                });

            migrationBuilder.CreateTable(
                name: "TuitionFeeStatuses",
                columns: table => new
                {
                    TuitionFeeStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuitionFeeStatuses", x => x.TuitionFeeStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    StudentNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PassportId = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HighestDegree = table.Column<string>(type: "TEXT", nullable: true),
                    LanguageResult = table.Column<string>(type: "TEXT", nullable: true),
                    YearsWorkExperience = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Students_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    MajorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.MajorId);
                    table.ForeignKey(
                        name: "FK_Majors_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentDocuments",
                columns: table => new
                {
                    StudentDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileUrl = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocuments", x => x.StudentDocumentId);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_AspNetUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeDocumentRequirements",
                columns: table => new
                {
                    ProgrammeDocumentRequirementId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MajorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeDocumentRequirements", x => x.ProgrammeDocumentRequirementId);
                    table.ForeignKey(
                        name: "FK_ProgrammeDocumentRequirements_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammeDocumentRequirements_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammeDocumentRequirements_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                columns: table => new
                {
                    StudentEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgrammeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MajorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommencementDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModeOfStudyId = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationOfStudyMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    PathwayId = table.Column<int>(type: "INTEGER", nullable: true),
                    OfferType = table.Column<string>(type: "TEXT", nullable: true),
                    PaymentDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AdmissionConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MissingDocsSubmitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CertReleased = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnrollmentStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    TranscriptReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TuitionFeeStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    OtherFeesStatus = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.StudentEnrollmentId);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_EnrollmentStatuses_EnrollmentStatusId",
                        column: x => x.EnrollmentStatusId,
                        principalTable: "EnrollmentStatuses",
                        principalColumn: "EnrollmentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_ModesOfStudy_ModeOfStudyId",
                        column: x => x.ModeOfStudyId,
                        principalTable: "ModesOfStudy",
                        principalColumn: "ModeOfStudyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Pathways_PathwayId",
                        column: x => x.PathwayId,
                        principalTable: "Pathways",
                        principalColumn: "PathwayId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_TuitionFeeStatuses_TuitionFeeStatusId",
                        column: x => x.TuitionFeeStatusId,
                        principalTable: "TuitionFeeStatuses",
                        principalColumn: "TuitionFeeStatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MajorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Ects = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectId);
                    table.ForeignKey(
                        name: "FK_Subjects_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentDocuments",
                columns: table => new
                {
                    EnrollmentDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentDocuments", x => x.EnrollmentDocumentId);
                    table.ForeignKey(
                        name: "FK_EnrollmentDocuments_AspNetUsers_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EnrollmentDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "DocumentTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentDocuments_StudentDocuments_StudentDocumentId",
                        column: x => x.StudentDocumentId,
                        principalTable: "StudentDocuments",
                        principalColumn: "StudentDocumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentDocuments_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinalProjects",
                columns: table => new
                {
                    FinalProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Supervisor = table.Column<string>(type: "TEXT", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinalProjectStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalProjects", x => x.FinalProjectId);
                    table.ForeignKey(
                        name: "FK_FinalProjects_FinalProjectStatuses_FinalProjectStatusId",
                        column: x => x.FinalProjectStatusId,
                        principalTable: "FinalProjectStatuses",
                        principalColumn: "FinalProjectStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinalProjects_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentNotes",
                columns: table => new
                {
                    StudentNoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentNotes", x => x.StudentNoteId);
                    table.ForeignKey(
                        name: "FK_StudentNotes_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentNotes_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubjectGrades",
                columns: table => new
                {
                    SubjectGradeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentEnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectGrades", x => x.SubjectGradeId);
                    table.ForeignKey(
                        name: "FK_SubjectGrades_StudentEnrollments_StudentEnrollmentId",
                        column: x => x.StudentEnrollmentId,
                        principalTable: "StudentEnrollments",
                        principalColumn: "StudentEnrollmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectGrades_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DocumentTypes",
                columns: new[] { "DocumentTypeId", "DeletedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Passport" },
                    { 2, null, null, "CV" },
                    { 3, null, null, "Degree Certificate" },
                    { 4, null, null, "Language Certificate" },
                    { 5, null, null, "Other" }
                });

            migrationBuilder.InsertData(
                table: "EnrollmentStatuses",
                columns: new[] { "EnrollmentStatusId", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, null, "Active" },
                    { 2, null, "Active (Final Project)" },
                    { 3, null, "Applicant Withdraw" },
                    { 4, null, "Cancelled" },
                    { 5, null, "Deferred" },
                    { 6, null, "Dismissed" },
                    { 7, null, "Drop Out" },
                    { 8, null, "Graduated" },
                    { 9, null, "Potential Applicant" },
                    { 10, null, "Potential Applicant Paid" },
                    { 11, null, "Transferred" }
                });

            migrationBuilder.InsertData(
                table: "FinalProjectStatuses",
                columns: new[] { "FinalProjectStatusId", "DeletedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, null, null, "Not Started" },
                    { 2, null, null, "In Progress" },
                    { 3, null, null, "Submitted" },
                    { 4, null, null, "Passed" },
                    { 5, null, null, "Failed" }
                });

            migrationBuilder.InsertData(
                table: "ModesOfStudy",
                columns: new[] { "ModeOfStudyId", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, null, "Distance/Online Self-Study" },
                    { 2, null, "Blended Learning" },
                    { 3, null, "Full-Time On-Campus" }
                });

            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "PathwayId", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, null, "Pathway 1: Direct Entry via Bachelor's Degree" },
                    { 2, null, "Pathway 2: Advanced Diploma + 3 Years Work Exp" },
                    { 3, null, "Pathway 3: Diploma + 5 Years Work Exp" },
                    { 4, null, "Pathway 4: High School + 8 Years Work Exp" },
                    { 5, null, "Pathway 1: Master's Degree (Preferred Entry)" },
                    { 6, null, "Pathway 2: Bachelor's Degree + 5 Years Work Exp" },
                    { 7, null, "Pathway 3: Advanced Diploma + 7 Years Work Exp" },
                    { 8, null, "Pathway 4: Diploma + 9 Years Work Exp" },
                    { 9, null, "Pathway 5: High School + 12 Years Work Exp" },
                    { 10, null, "Open Entry" },
                    { 11, null, "Pathway 1: Diploma or Associate Degree" },
                    { 12, null, "Pathway 2: High School Certificate + 3 Years Work Exp" },
                    { 13, null, "Pathway 1: Advanced Diploma" },
                    { 14, null, "Pathway 2: Diploma + 2 Years Work Exp" },
                    { 15, null, "Pathway 3: High School Certificate + 5 Years Work Exp" }
                });

            migrationBuilder.InsertData(
                table: "TuitionFeeStatuses",
                columns: new[] { "TuitionFeeStatusId", "DeletedAt", "Name" },
                values: new object[,]
                {
                    { 1, null, "Unpaid" },
                    { 2, null, "Partially Paid" },
                    { 3, null, "Fully Paid" },
                    { 4, null, "Not Applicable" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDocuments_ApprovedByUserId",
                table: "EnrollmentDocuments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDocuments_DocumentTypeId",
                table: "EnrollmentDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDocuments_StudentDocumentId",
                table: "EnrollmentDocuments",
                column: "StudentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentDocuments_StudentEnrollmentId",
                table: "EnrollmentDocuments",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalProjects_FinalProjectStatusId",
                table: "FinalProjects",
                column: "FinalProjectStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalProjects_StudentEnrollmentId",
                table: "FinalProjects",
                column: "StudentEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Majors_ProgrammeId",
                table: "Majors",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_UserId",
                table: "Partners",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeDocumentRequirements_DocumentTypeId",
                table: "ProgrammeDocumentRequirements",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeDocumentRequirements_MajorId",
                table: "ProgrammeDocumentRequirements",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeDocumentRequirements_ProgrammeId",
                table: "ProgrammeDocumentRequirements",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_Code",
                table: "Programmes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_DocumentTypeId",
                table: "StudentDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_StudentId",
                table: "StudentDocuments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_VerifiedByUserId",
                table: "StudentDocuments",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_EnrollmentStatusId",
                table: "StudentEnrollments",
                column: "EnrollmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_MajorId",
                table: "StudentEnrollments",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_ModeOfStudyId",
                table: "StudentEnrollments",
                column: "ModeOfStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_PathwayId",
                table: "StudentEnrollments",
                column: "PathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_ProgrammeId",
                table: "StudentEnrollments",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_StudentId",
                table: "StudentEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_TuitionFeeStatusId",
                table: "StudentEnrollments",
                column: "TuitionFeeStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_StudentEnrollmentId",
                table: "StudentNotes",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_StudentId",
                table: "StudentNotes",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_PartnerId",
                table: "Students",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGrades_StudentEnrollmentId",
                table: "SubjectGrades",
                column: "StudentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGrades_SubjectId",
                table: "SubjectGrades",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_MajorId",
                table: "Subjects",
                column: "MajorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentDocuments");

            migrationBuilder.DropTable(
                name: "FinalProjects");

            migrationBuilder.DropTable(
                name: "ProgrammeDocumentRequirements");

            migrationBuilder.DropTable(
                name: "StudentNotes");

            migrationBuilder.DropTable(
                name: "SubjectGrades");

            migrationBuilder.DropTable(
                name: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "FinalProjectStatuses");

            migrationBuilder.DropTable(
                name: "StudentEnrollments");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "EnrollmentStatuses");

            migrationBuilder.DropTable(
                name: "ModesOfStudy");

            migrationBuilder.DropTable(
                name: "Pathways");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "TuitionFeeStatuses");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Programmes");

            migrationBuilder.CreateTable(
                name: "AccessLevelDefinitions",
                columns: table => new
                {
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLevelDefinitions", x => x.Level);
                });

            migrationBuilder.CreateTable(
                name: "CaseLevelLabelOverrides",
                columns: table => new
                {
                    CaseLevelLabelOverrideId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseLevelLabelOverrides", x => x.CaseLevelLabelOverrideId);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.CaseId);
                });

            migrationBuilder.CreateTable(
                name: "CaseTeamKeys",
                columns: table => new
                {
                    CaseTeamKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTeamKeys", x => x.CaseTeamKeyId);
                });

            migrationBuilder.CreateTable(
                name: "CaseTeamMemberOverrides",
                columns: table => new
                {
                    CaseTeamMemberOverrideId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTeamMemberOverrides", x => x.CaseTeamMemberOverrideId);
                });

            migrationBuilder.CreateTable(
                name: "CaseTeamMemberships",
                columns: table => new
                {
                    CaseTeamMembershipId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTeamMemberships", x => x.CaseTeamMembershipId);
                });

            migrationBuilder.CreateTable(
                name: "CaseUserKeys",
                columns: table => new
                {
                    CaseUserKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseUserKeys", x => x.CaseUserKeyId);
                });

            migrationBuilder.CreateTable(
                name: "CaseUserMembers",
                columns: table => new
                {
                    CaseUserMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseUserMembers", x => x.CaseUserMemberId);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.TeamMemberId);
                });

            migrationBuilder.CreateTable(
                name: "TeamRoles",
                columns: table => new
                {
                    TeamRoleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRoles", x => x.TeamRoleId);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TeamTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamId);
                });

            migrationBuilder.CreateTable(
                name: "TeamTypes",
                columns: table => new
                {
                    TeamTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTypes", x => x.TeamTypeId);
                });

            migrationBuilder.CreateTable(
                name: "CaseFiles",
                columns: table => new
                {
                    CaseFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessMode = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFiles", x => x.CaseFileId);
                    table.ForeignKey(
                        name: "FK_CaseFiles_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseKeyPairs",
                columns: table => new
                {
                    CaseKeyPairId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseKeyPairs", x => x.CaseKeyPairId);
                    table.ForeignKey(
                        name: "FK_CaseKeyPairs_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamSymmetricKeys",
                columns: table => new
                {
                    TeamSymmetricKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EncryptedKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSymmetricKeys", x => x.TeamSymmetricKeyId);
                    table.ForeignKey(
                        name: "FK_TeamSymmetricKeys_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseFileLevelKeys",
                columns: table => new
                {
                    CaseFileLevelKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EncryptedFileKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseFileLevelKeys", x => x.CaseFileLevelKeyId);
                    table.ForeignKey(
                        name: "FK_CaseFileLevelKeys_CaseFiles_CaseFileId",
                        column: x => x.CaseFileId,
                        principalTable: "CaseFiles",
                        principalColumn: "CaseFileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccessLevelDefinitions",
                columns: new[] { "Level", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Case owner with full authority over all case resources.", "Lead Partner" },
                    { 2, "Senior lawyers actively working the case.", "Senior Associate" },
                    { 3, "General legal team members on the case.", "Associate" },
                    { 4, "External party with limited view of relevant documents.", "Client" },
                    { 5, "Internal support staff handling document preparation.", "Paralegal" },
                    { 6, "Experts, witnesses, or third parties with restricted access.", "External Consultant" }
                });

            migrationBuilder.InsertData(
                table: "TeamRoles",
                columns: new[] { "TeamRoleId", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("51c27b7b-e12e-dcfe-d76b-dc68926d4f7b"), "Full control of the team.", "Administrator" },
                    { new Guid("76a48b85-535e-12c7-ef67-2a9570474b1d"), "Standard team member.", "Member" }
                });

            migrationBuilder.InsertData(
                table: "TeamTypes",
                columns: new[] { "TeamTypeId", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("318a0cc9-c8d3-7aad-dab8-7111e02c4bb8"), "Infrastructure and operations team.", "Operations" },
                    { new Guid("c861eb2f-fb48-7bdb-f2a0-083f918d83a9"), "Leadership and management team.", "Management" },
                    { new Guid("d5d4b942-cabe-8fd8-e4f2-adfc5b579f72"), "Customer-facing support team.", "Support" },
                    { new Guid("ea8585f1-249f-93d3-4993-2a9a907c5175"), "Software development team.", "Development" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseFileLevelKeys_CaseFileId_Level",
                table: "CaseFileLevelKeys",
                columns: new[] { "CaseFileId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseFiles_CaseId",
                table: "CaseFiles",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseKeyPairs_CaseId_Level",
                table: "CaseKeyPairs",
                columns: new[] { "CaseId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseLevelLabelOverrides_CaseId_Level",
                table: "CaseLevelLabelOverrides",
                columns: new[] { "CaseId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseTeamKeys_CaseId_TeamId_Level",
                table: "CaseTeamKeys",
                columns: new[] { "CaseId", "TeamId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseTeamMemberOverrides_CaseId_TeamId_UserId",
                table: "CaseTeamMemberOverrides",
                columns: new[] { "CaseId", "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseTeamMemberships_CaseId_TeamId",
                table: "CaseTeamMemberships",
                columns: new[] { "CaseId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseUserKeys_CaseId_UserId_Level",
                table: "CaseUserKeys",
                columns: new[] { "CaseId", "UserId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseUserMembers_CaseId_UserId",
                table: "CaseUserMembers",
                columns: new[] { "CaseId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamSymmetricKeys_TeamId_UserId",
                table: "TeamSymmetricKeys",
                columns: new[] { "TeamId", "UserId" },
                unique: true);
        }
    }
}
