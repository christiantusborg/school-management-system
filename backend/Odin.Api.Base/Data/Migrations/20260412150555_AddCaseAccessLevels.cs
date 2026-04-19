using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseAccessLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessLevelDefinitions",
                columns: table => new
                {
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLevelDefinitions", x => x.Level);
                });

            migrationBuilder.CreateTable(
                name: "CaseFiles",
                columns: table => new
                {
                    CaseFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    MinLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessMode = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                name: "CaseLevelLabelOverrides",
                columns: table => new
                {
                    CaseLevelLabelOverrideId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseLevelLabelOverrides", x => x.CaseLevelLabelOverrideId);
                });

            migrationBuilder.CreateTable(
                name: "CaseTeamKeys",
                columns: table => new
                {
                    CaseTeamKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "TEXT", nullable: false),
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedLevelPrivKey = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseUserMembers", x => x.CaseUserMemberId);
                });

            migrationBuilder.CreateTable(
                name: "CaseFileLevelKeys",
                columns: table => new
                {
                    CaseFileLevelKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseFileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    KemCiphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    EncryptedFileKey = table.Column<byte[]>(type: "BLOB", nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "CaseFiles");
        }
    }
}
