using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAttachment",
                schema: "public",
                table: "documents");

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                schema: "public",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentType",
                schema: "public",
                table: "documents");

            migrationBuilder.AddColumn<bool>(
                name: "IsAttachment",
                schema: "public",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
