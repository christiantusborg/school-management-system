using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Odin.Api.Base.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerDisabledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAt",
                table: "Partners",
                type: "timestamp without time zone",
                nullable: true);

            // Backfill: until this migration the Disable endpoint set
            // DeletedAt instead of DisabledAt — there was no separate
            // disabled state. Every pre-existing "deleted" row was actually
            // a disabled partner. Move those timestamps to DisabledAt and
            // clear DeletedAt so the new three-state model (active /
            // disabled / deleted) starts from a coherent baseline.
            migrationBuilder.Sql(@"
                UPDATE ""Partners""
                SET ""DisabledAt"" = ""DeletedAt"",
                    ""DeletedAt"" = NULL
                WHERE ""DeletedAt"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "Partners");
        }
    }
}
