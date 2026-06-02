using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddMeldingShortId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortId",
                schema: "public",
                table: "meldinger",
                type: "text",
                nullable: false,
                computedColumnSql: "right(\"Id\"::text, 12)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_meldinger_ShortId",
                schema: "public",
                table: "meldinger",
                column: "ShortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_meldinger_ShortId",
                schema: "public",
                table: "meldinger");

            migrationBuilder.DropColumn(
                name: "ShortId",
                schema: "public",
                table: "meldinger");
        }
    }
}
