using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class MailHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailAccounts",
                columns: table => new
                {
                    MailAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImapHost = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ImapPort = table.Column<int>(type: "integer", nullable: false),
                    ImapUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SmtpUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    Username = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAccounts", x => x.MailAccountId);
                });

            migrationBuilder.CreateTable(
                name: "MailAccountAccesses",
                columns: table => new
                {
                    MailAccountAccessId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAccountAccesses", x => x.MailAccountAccessId);
                    table.ForeignKey(
                        name: "FK_MailAccountAccesses_MailAccounts_MailAccountId",
                        column: x => x.MailAccountId,
                        principalTable: "MailAccounts",
                        principalColumn: "MailAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailFolders",
                columns: table => new
                {
                    MailFolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    UidValidity = table.Column<long>(type: "bigint", nullable: false),
                    LastSeenUid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailFolders", x => x.MailFolderId);
                    table.ForeignKey(
                        name: "FK_MailFolders_MailAccounts_MailAccountId",
                        column: x => x.MailAccountId,
                        principalTable: "MailAccounts",
                        principalColumn: "MailAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailMessages",
                columns: table => new
                {
                    MailMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImapUid = table.Column<long>(type: "bigint", nullable: false),
                    MessageIdHeader = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InReplyTo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    FromName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ToAddresses = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CcAddresses = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BodyText = table.Column<string>(type: "text", nullable: true),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    IsOutbound = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailMessages", x => x.MailMessageId);
                    table.ForeignKey(
                        name: "FK_MailMessages_MailAccounts_MailAccountId",
                        column: x => x.MailAccountId,
                        principalTable: "MailAccounts",
                        principalColumn: "MailAccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MailMessages_MailFolders_MailFolderId",
                        column: x => x.MailFolderId,
                        principalTable: "MailFolders",
                        principalColumn: "MailFolderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailAttachments",
                columns: table => new
                {
                    MailAttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: true),
                    Skipped = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAttachments", x => x.MailAttachmentId);
                    table.ForeignKey(
                        name: "FK_MailAttachments_MailMessages_MailMessageId",
                        column: x => x.MailMessageId,
                        principalTable: "MailMessages",
                        principalColumn: "MailMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MailMessageLinks",
                columns: table => new
                {
                    MailMessageLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    MailMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CrmLeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchedAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailMessageLinks", x => x.MailMessageLinkId);
                    table.ForeignKey(
                        name: "FK_MailMessageLinks_MailMessages_MailMessageId",
                        column: x => x.MailMessageId,
                        principalTable: "MailMessages",
                        principalColumn: "MailMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailAccountAccesses_MailAccountId_UserId",
                table: "MailAccountAccesses",
                columns: new[] { "MailAccountId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachments_MailMessageId",
                table: "MailAttachments",
                column: "MailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MailFolders_MailAccountId_Name",
                table: "MailFolders",
                columns: new[] { "MailAccountId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailMessageLinks_CrmLeadId",
                table: "MailMessageLinks",
                column: "CrmLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessageLinks_MailMessageId",
                table: "MailMessageLinks",
                column: "MailMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessageLinks_PartnerId",
                table: "MailMessageLinks",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessageLinks_StudentId",
                table: "MailMessageLinks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_FromAddress",
                table: "MailMessages",
                column: "FromAddress");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_MailAccountId",
                table: "MailMessages",
                column: "MailAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MailMessages_MailFolderId_ImapUid",
                table: "MailMessages",
                columns: new[] { "MailFolderId", "ImapUid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailAccountAccesses");

            migrationBuilder.DropTable(
                name: "MailAttachments");

            migrationBuilder.DropTable(
                name: "MailMessageLinks");

            migrationBuilder.DropTable(
                name: "MailMessages");

            migrationBuilder.DropTable(
                name: "MailFolders");

            migrationBuilder.DropTable(
                name: "MailAccounts");
        }
    }
}
