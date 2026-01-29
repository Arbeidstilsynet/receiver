using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReceiverSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "public");

            migrationBuilder.RenameTable(
                name: "registered_altinn_apps",
                schema: "receiver",
                newName: "registered_altinn_apps",
                newSchema: "public"
            );

            migrationBuilder.RenameTable(
                name: "meldinger",
                schema: "receiver",
                newName: "meldinger",
                newSchema: "public"
            );

            migrationBuilder.RenameTable(
                name: "documents",
                schema: "receiver",
                newName: "documents",
                newSchema: "public"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "receiver");

            migrationBuilder.RenameTable(
                name: "registered_altinn_apps",
                schema: "public",
                newName: "registered_altinn_apps",
                newSchema: "receiver"
            );

            migrationBuilder.RenameTable(
                name: "meldinger",
                schema: "public",
                newName: "meldinger",
                newSchema: "receiver"
            );

            migrationBuilder.RenameTable(
                name: "documents",
                schema: "public",
                newName: "documents",
                newSchema: "receiver"
            );
        }
    }
}
