using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartnerContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMethodTypes",
                columns: table => new
                {
                    ContactMethodTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMethodTypes", x => x.ContactMethodTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContactTypes",
                columns: table => new
                {
                    PartnerContactTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContactTypes", x => x.PartnerContactTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContacts",
                columns: table => new
                {
                    PartnerContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerContactTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContacts", x => x.PartnerContactId);
                    table.ForeignKey(
                        name: "FK_PartnerContacts_PartnerContactTypes_PartnerContactTypeId",
                        column: x => x.PartnerContactTypeId,
                        principalTable: "PartnerContactTypes",
                        principalColumn: "PartnerContactTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerContacts_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerContactMethods",
                columns: table => new
                {
                    PartnerContactMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactMethodTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerContactMethods", x => x.PartnerContactMethodId);
                    table.ForeignKey(
                        name: "FK_PartnerContactMethods_ContactMethodTypes_ContactMethodTypeId",
                        column: x => x.ContactMethodTypeId,
                        principalTable: "ContactMethodTypes",
                        principalColumn: "ContactMethodTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartnerContactMethods_PartnerContacts_PartnerContactId",
                        column: x => x.PartnerContactId,
                        principalTable: "PartnerContacts",
                        principalColumn: "PartnerContactId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContactMethods_ContactMethodTypeId",
                table: "PartnerContactMethods",
                column: "ContactMethodTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContactMethods_PartnerContactId",
                table: "PartnerContactMethods",
                column: "PartnerContactId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContacts_PartnerContactTypeId",
                table: "PartnerContacts",
                column: "PartnerContactTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerContacts_PartnerId",
                table: "PartnerContacts",
                column: "PartnerId");

            // Seed common worldwide contact methods; only Email, Phone and
            // WhatsApp start enabled (DeletedAt doubles as the disable flag).
            migrationBuilder.Sql("""
                INSERT INTO "ContactMethodTypes" ("ContactMethodTypeId", "Name", "DisplayOrder", "DeletedAt")
                VALUES
                    (gen_random_uuid(), 'Email',              10, NULL),
                    (gen_random_uuid(), 'Phone',              20, NULL),
                    (gen_random_uuid(), 'WhatsApp',           30, NULL),
                    (gen_random_uuid(), 'SMS',                40, now()),
                    (gen_random_uuid(), 'WeChat',             50, now()),
                    (gen_random_uuid(), 'Telegram',           60, now()),
                    (gen_random_uuid(), 'Viber',              70, now()),
                    (gen_random_uuid(), 'LINE',               80, now()),
                    (gen_random_uuid(), 'Facebook Messenger', 90, now()),
                    (gen_random_uuid(), 'Skype',             100, now()),
                    (gen_random_uuid(), 'Zoom',              110, now()),
                    (gen_random_uuid(), 'Signal',            120, now()),
                    (gen_random_uuid(), 'LinkedIn',          130, now()),
                    (gen_random_uuid(), 'iMessage',          140, now()),
                    (gen_random_uuid(), 'KakaoTalk',         150, now()),
                    (gen_random_uuid(), 'Zalo',              160, now()),
                    (gen_random_uuid(), 'IMO',               170, now()),
                    (gen_random_uuid(), 'Botim',             180, now());

                INSERT INTO "PartnerContactTypes" ("PartnerContactTypeId", "Name", "DisplayOrder", "DeletedAt")
                VALUES
                    (gen_random_uuid(), 'Owner',     10, NULL),
                    (gen_random_uuid(), 'Admission', 20, NULL),
                    (gen_random_uuid(), 'Marketing', 30, NULL),
                    (gen_random_uuid(), 'Finance',   40, NULL);

                -- Convert each partner's legacy single contact into an
                -- Owner-typed contact carrying its primary email and phone.
                INSERT INTO "PartnerContacts" ("PartnerContactId", "PartnerId", "PartnerContactTypeId", "Name", "SortOrder")
                SELECT gen_random_uuid(), p."PartnerId",
                       (SELECT t."PartnerContactTypeId" FROM "PartnerContactTypes" t WHERE t."Name" = 'Owner' LIMIT 1),
                       COALESCE(NULLIF(TRIM(p."ContactPersonName"), ''), 'Primary contact'),
                       0
                FROM "Partners" p
                WHERE p."DeletedAt" IS NULL
                  AND (NULLIF(TRIM(p."ContactPersonName"), '') IS NOT NULL
                       OR EXISTS (SELECT 1 FROM "PartnerContactEmails" e
                                  WHERE e."PartnerId" = p."PartnerId" AND e."DeletedAt" IS NULL AND NULLIF(TRIM(e."Email"), '') IS NOT NULL));

                INSERT INTO "PartnerContactMethods" ("PartnerContactMethodId", "PartnerContactId", "ContactMethodTypeId", "Value")
                SELECT gen_random_uuid(), c."PartnerContactId",
                       (SELECT m."ContactMethodTypeId" FROM "ContactMethodTypes" m WHERE m."Name" = 'Email' LIMIT 1),
                       e."Email"
                FROM "PartnerContacts" c
                JOIN LATERAL (
                    SELECT e2."Email" FROM "PartnerContactEmails" e2
                    WHERE e2."PartnerId" = c."PartnerId" AND e2."DeletedAt" IS NULL AND NULLIF(TRIM(e2."Email"), '') IS NOT NULL
                    ORDER BY e2."IsPrimary" DESC LIMIT 1
                ) e ON true;

                INSERT INTO "PartnerContactMethods" ("PartnerContactMethodId", "PartnerContactId", "ContactMethodTypeId", "Value")
                SELECT gen_random_uuid(), c."PartnerContactId",
                       (SELECT m."ContactMethodTypeId" FROM "ContactMethodTypes" m WHERE m."Name" = 'Phone' LIMIT 1),
                       ph."Phone"
                FROM "PartnerContacts" c
                JOIN LATERAL (
                    SELECT p2."Phone" FROM "PartnerContactPhones" p2
                    WHERE p2."PartnerId" = c."PartnerId" AND p2."DeletedAt" IS NULL AND NULLIF(TRIM(p2."Phone"), '') IS NOT NULL
                    ORDER BY p2."IsPrimary" DESC LIMIT 1
                ) ph ON true;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerContactMethods");

            migrationBuilder.DropTable(
                name: "ContactMethodTypes");

            migrationBuilder.DropTable(
                name: "PartnerContacts");

            migrationBuilder.DropTable(
                name: "PartnerContactTypes");
        }
    }
}
