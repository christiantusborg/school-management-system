using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class CrmLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmAssignmentConfigs",
                columns: table => new
                {
                    CrmAssignmentConfigId = table.Column<int>(type: "integer", nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    MemberUserIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LastAssignedIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmAssignmentConfigs", x => x.CrmAssignmentConfigId);
                });

            migrationBuilder.CreateTable(
                name: "CrmLeadSources",
                columns: table => new
                {
                    CrmLeadSourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmLeadSources", x => x.CrmLeadSourceId);
                });

            migrationBuilder.CreateTable(
                name: "CrmPipelines",
                columns: table => new
                {
                    CrmPipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmPipelines", x => x.CrmPipelineId);
                });

            migrationBuilder.CreateTable(
                name: "CrmStages",
                columns: table => new
                {
                    CrmStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrmPipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    StageType = table.Column<int>(type: "integer", nullable: false),
                    SlaHours = table.Column<int>(type: "integer", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmStages", x => x.CrmStageId);
                    table.ForeignKey(
                        name: "FK_CrmStages_CrmPipelines_CrmPipelineId",
                        column: x => x.CrmPipelineId,
                        principalTable: "CrmPipelines",
                        principalColumn: "CrmPipelineId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmLeads",
                columns: table => new
                {
                    CrmLeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrmPipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrmStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CrmLeadSourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgrammeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValueBand = table.Column<int>(type: "integer", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    StageEnteredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConvertedStudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LostReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmLeads", x => x.CrmLeadId);
                    table.ForeignKey(
                        name: "FK_CrmLeads_CrmLeadSources_CrmLeadSourceId",
                        column: x => x.CrmLeadSourceId,
                        principalTable: "CrmLeadSources",
                        principalColumn: "CrmLeadSourceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CrmLeads_CrmPipelines_CrmPipelineId",
                        column: x => x.CrmPipelineId,
                        principalTable: "CrmPipelines",
                        principalColumn: "CrmPipelineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmLeads_CrmStages_CrmStageId",
                        column: x => x.CrmStageId,
                        principalTable: "CrmStages",
                        principalColumn: "CrmStageId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmActivities",
                columns: table => new
                {
                    CrmActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CrmLeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DueAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmActivities", x => x.CrmActivityId);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmLeads_CrmLeadId",
                        column: x => x.CrmLeadId,
                        principalTable: "CrmLeads",
                        principalColumn: "CrmLeadId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_CrmLeadId_DueAt",
                table: "CrmActivities",
                columns: new[] { "CrmLeadId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_AssignedToUserId",
                table: "CrmLeads",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_CrmLeadSourceId",
                table: "CrmLeads",
                column: "CrmLeadSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_CrmPipelineId",
                table: "CrmLeads",
                column: "CrmPipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_CrmStageId",
                table: "CrmLeads",
                column: "CrmStageId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmLeads_Email",
                table: "CrmLeads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_CrmStages_CrmPipelineId",
                table: "CrmStages",
                column: "CrmPipelineId");

            // Seed the default recruitment pipeline, stages and lead sources.
            migrationBuilder.Sql("""
                INSERT INTO "CrmPipelines" ("CrmPipelineId", "Name", "IsDefault", "DisplayOrder", "DeletedAt")
                VALUES ('11111111-1111-1111-1111-c00000000001', 'Student Recruitment', true, 1, NULL);

                INSERT INTO "CrmStages" ("CrmStageId", "CrmPipelineId", "Name", "Color", "DisplayOrder", "StageType", "SlaHours", "DeletedAt")
                VALUES
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'New',        '#1058a4', 1, 0, 24,   NULL),
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'Contacted',  '#7a5c00', 2, 0, 48,   NULL),
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'Interested', '#1c7a4a', 3, 0, NULL, NULL),
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'Applying',   '#6b21a8', 4, 0, NULL, NULL),
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'Enrolled',   '#14532d', 5, 1, NULL, NULL),
                    (gen_random_uuid(), '11111111-1111-1111-1111-c00000000001', 'Lost',       '#8a1515', 6, 2, NULL, NULL);

                INSERT INTO "CrmLeadSources" ("CrmLeadSourceId", "Name", "DisplayOrder", "DeletedAt")
                VALUES
                    (gen_random_uuid(), 'Website',             10, NULL),
                    (gen_random_uuid(), 'Facebook',            20, NULL),
                    (gen_random_uuid(), 'Instagram',           30, NULL),
                    (gen_random_uuid(), 'LinkedIn',            40, NULL),
                    (gen_random_uuid(), 'WhatsApp inbound',    50, NULL),
                    (gen_random_uuid(), 'Referral — Partner',  60, NULL),
                    (gen_random_uuid(), 'Referral — Student',  70, NULL),
                    (gen_random_uuid(), 'Education fair / event', 80, NULL),
                    (gen_random_uuid(), 'Agent',               90, NULL),
                    (gen_random_uuid(), 'Walk-in',            100, NULL),
                    (gen_random_uuid(), 'Other',              110, NULL);

                INSERT INTO "CrmAssignmentConfigs" ("CrmAssignmentConfigId", "Strategy", "MemberUserIds", "LastAssignedIndex")
                VALUES (1, 0, NULL, -1);
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmActivities");

            migrationBuilder.DropTable(
                name: "CrmAssignmentConfigs");

            migrationBuilder.DropTable(
                name: "CrmLeads");

            migrationBuilder.DropTable(
                name: "CrmLeadSources");

            migrationBuilder.DropTable(
                name: "CrmStages");

            migrationBuilder.DropTable(
                name: "CrmPipelines");
        }
    }
}
