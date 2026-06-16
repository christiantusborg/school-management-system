using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerPartnerLetterTemplates : Migration
    {
        // Owning (programme, partner) pairs: each partner a shared core programme
        // is granted to (ProgrammePartners), plus the owner of a partner-owned
        // programme (Programmes.OwnerId). Used to fan shared templates out.
        private const string PairsCte = @"
            WITH pairs AS (
                SELECT ""ProgrammeId"", ""PartnerId""
                FROM ""ProgrammePartners"" WHERE ""IsActive"" IS NOT NULL
                UNION
                SELECT ""ProgrammeId"", ""OwnerId"" AS ""PartnerId""
                FROM ""Programmes"" WHERE ""OwnerId"" IS NOT NULL AND ""DeletedAt"" IS NULL
            )";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old per-(programme, type) unique indexes before fanning
            // rows out, since the backfill creates multiple rows per programme.
            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_ProgrammeId_LetterType",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_LetterType",
                table: "LetterEmailTemplates");

            // Add PartnerId nullable first so existing rows can be backfilled.
            migrationBuilder.AddColumn<Guid>(
                name: "PartnerId",
                table: "LetterTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerId",
                table: "LetterEmailTemplates",
                type: "uuid",
                nullable: true);

            // Backfill: replicate each existing (active) shared template to one
            // row per owning partner, then drop the partnerless originals (and
            // any template for a programme no partner offers — those are dead).
            migrationBuilder.Sql(PairsCte + @"
                INSERT INTO ""LetterTemplates""
                    (""LetterTemplateId"", ""ProgrammeId"", ""PartnerId"", ""LetterType"",
                     ""BodyHtml"", ""CertificateBackgroundPath"", ""CertificateLayoutJson"",
                     ""IsPublished"", ""UpdatedAt"", ""UpdatedByUserId"", ""DeletedAt"")
                SELECT gen_random_uuid(), t.""ProgrammeId"", pr.""PartnerId"", t.""LetterType"",
                       t.""BodyHtml"", t.""CertificateBackgroundPath"", t.""CertificateLayoutJson"",
                       t.""IsPublished"", t.""UpdatedAt"", t.""UpdatedByUserId"", t.""DeletedAt""
                FROM ""LetterTemplates"" t
                JOIN pairs pr ON pr.""ProgrammeId"" = t.""ProgrammeId""
                WHERE t.""PartnerId"" IS NULL AND t.""DeletedAt"" IS NULL;");
            migrationBuilder.Sql(@"DELETE FROM ""LetterTemplates"" WHERE ""PartnerId"" IS NULL;");

            migrationBuilder.Sql(PairsCte + @"
                INSERT INTO ""LetterEmailTemplates""
                    (""LetterEmailTemplateId"", ""ProgrammeId"", ""PartnerId"", ""LetterType"",
                     ""IsEmailEnabled"", ""Subject"", ""BodyHtml"", ""CcRecipientsJson"",
                     ""BccRecipientsJson"", ""UpdatedAt"", ""UpdatedByUserId"", ""DeletedAt"")
                SELECT gen_random_uuid(), t.""ProgrammeId"", pr.""PartnerId"", t.""LetterType"",
                       t.""IsEmailEnabled"", t.""Subject"", t.""BodyHtml"", t.""CcRecipientsJson"",
                       t.""BccRecipientsJson"", t.""UpdatedAt"", t.""UpdatedByUserId"", t.""DeletedAt""
                FROM ""LetterEmailTemplates"" t
                JOIN pairs pr ON pr.""ProgrammeId"" = t.""ProgrammeId""
                WHERE t.""PartnerId"" IS NULL AND t.""DeletedAt"" IS NULL;");
            migrationBuilder.Sql(@"DELETE FROM ""LetterEmailTemplates"" WHERE ""PartnerId"" IS NULL;");

            // Every surviving row now has a partner: enforce NOT NULL.
            migrationBuilder.AlterColumn<Guid>(
                name: "PartnerId",
                table: "LetterTemplates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PartnerId",
                table: "LetterEmailTemplates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_PartnerId",
                table: "LetterTemplates",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_PartnerId",
                table: "LetterEmailTemplates",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "PartnerId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_LetterEmailTemplates_Partners_PartnerId",
                table: "LetterEmailTemplates",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LetterTemplates_Partners_PartnerId",
                table: "LetterTemplates",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "PartnerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LetterEmailTemplates_Partners_PartnerId",
                table: "LetterEmailTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_LetterTemplates_Partners_PartnerId",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_PartnerId",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_PartnerId",
                table: "LetterEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_PartnerId_LetterType",
                table: "LetterEmailTemplates");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "LetterTemplates");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "LetterEmailTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_LetterTemplates_ProgrammeId_LetterType",
                table: "LetterTemplates",
                columns: new[] { "ProgrammeId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LetterEmailTemplates_ProgrammeId_LetterType",
                table: "LetterEmailTemplates",
                columns: new[] { "ProgrammeId", "LetterType" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }
    }
}
