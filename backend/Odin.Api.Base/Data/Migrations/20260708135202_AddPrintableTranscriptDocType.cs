using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintableTranscriptDocType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename the existing Transcript document type to "Digital Transcript"
            // and add the Admission-only "Printable Transcript" document type.
            migrationBuilder.Sql(@"
                UPDATE ""DocumentTypes""
                SET ""Name"" = 'Digital Transcript',
                    ""Description"" = 'System-generated transcript PDF for the partner and student.'
                WHERE ""DocumentTypeId"" = '22222222-2222-2222-2222-100000000003';

                INSERT INTO ""DocumentTypes"" (""DocumentTypeId"", ""Name"", ""Description"", ""IsSystemGenerated"")
                VALUES ('22222222-2222-2222-2222-100000000006',
                        'Printable Transcript',
                        'System-generated transcript PDF for the Admission Office only.',
                        TRUE)
                ON CONFLICT (""DocumentTypeId"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
