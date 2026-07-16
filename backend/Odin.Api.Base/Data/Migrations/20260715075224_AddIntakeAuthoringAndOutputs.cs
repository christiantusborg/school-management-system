using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntakeAuthoringAndOutputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FieldLibraryEntries",
                columns: table => new
                {
                    FieldLibraryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldLibraryEntries", x => x.FieldLibraryEntryId);
                });

            migrationBuilder.CreateTable(
                name: "GenerationRules",
                columns: table => new
                {
                    GenerationRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RuleJson = table.Column<string>(type: "text", nullable: false),
                    IncludeDocumentTemplateIdsCsv = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationRules", x => x.GenerationRuleId);
                });

            migrationBuilder.CreateTable(
                name: "IntakeDocumentTemplateImages",
                columns: table => new
                {
                    DocumentTemplateImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DataBase64 = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeDocumentTemplateImages", x => x.DocumentTemplateImageId);
                });

            migrationBuilder.CreateTable(
                name: "IntakeDocumentTemplates",
                columns: table => new
                {
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    BaseAssetRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MappingJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeDocumentTemplates", x => x.DocumentTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "IntakeTextTemplates",
                columns: table => new
                {
                    TextTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BodyJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeTextTemplates", x => x.TextTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "IntakeDocumentTemplateAssets",
                columns: table => new
                {
                    DocumentTemplateAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeDocumentTemplateAssets", x => x.DocumentTemplateAssetId);
                    table.ForeignKey(
                        name: "FK_IntakeDocumentTemplateAssets_IntakeDocumentTemplates_Docume~",
                        column: x => x.DocumentTemplateId,
                        principalTable: "IntakeDocumentTemplates",
                        principalColumn: "DocumentTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntakeOutputs",
                columns: table => new
                {
                    IntakeOutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutputKind = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeOutputs", x => x.IntakeOutputId);
                    table.ForeignKey(
                        name: "FK_IntakeOutputs_IntakeDocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalTable: "IntakeDocumentTemplates",
                        principalColumn: "DocumentTemplateId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IntakeOutputs_IntakeResponses_IntakeResponseId",
                        column: x => x.IntakeResponseId,
                        principalTable: "IntakeResponses",
                        principalColumn: "IntakeResponseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntakeDocumentTemplateAssets_DocumentTemplateId",
                table: "IntakeDocumentTemplateAssets",
                column: "DocumentTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeOutputs_DocumentTemplateId",
                table: "IntakeOutputs",
                column: "DocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_IntakeOutputs_IntakeResponseId",
                table: "IntakeOutputs",
                column: "IntakeResponseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldLibraryEntries");

            migrationBuilder.DropTable(
                name: "GenerationRules");

            migrationBuilder.DropTable(
                name: "IntakeDocumentTemplateAssets");

            migrationBuilder.DropTable(
                name: "IntakeDocumentTemplateImages");

            migrationBuilder.DropTable(
                name: "IntakeOutputs");

            migrationBuilder.DropTable(
                name: "IntakeTextTemplates");

            migrationBuilder.DropTable(
                name: "IntakeDocumentTemplates");
        }
    }
}
