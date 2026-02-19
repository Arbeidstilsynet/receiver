using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Tags",
                schema: "public",
                table: "documents",
                type: "hstore",
                nullable: false,
                defaultValueSql: "''::hstore"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Tags", schema: "public", table: "documents");
        }
    }
}
