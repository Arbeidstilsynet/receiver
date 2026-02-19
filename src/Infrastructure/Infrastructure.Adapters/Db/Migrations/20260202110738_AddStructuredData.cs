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
            // 1. Add DocumentType column with default value
            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                schema: "public",
                table: "documents",
                type: "varchar(24)",
                nullable: false,
                defaultValue: "Attachment"
            );

            // 2. Migrate IsAttachment data to DocumentType with logic. MainDocument will need to be moved manually
            migrationBuilder.Sql(
                @"
                UPDATE ""public"".""documents"" d
                SET ""DocumentType"" =
                    CASE
                        WHEN d.""IsAttachment"" = FALSE THEN 'StructuredData'
                        ELSE 'Attachment'
                    END
                FROM ""public"".""meldinger"" m
                WHERE d.""MeldingId"" = m.""Id"";
            "
            );

            // 3. Drop IsAttachment column
            migrationBuilder.DropColumn(name: "IsAttachment", schema: "public", table: "documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Add IsAttachment column with default value
            migrationBuilder.AddColumn<bool>(
                name: "IsAttachment",
                schema: "public",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            // 2. Migrate DocumentType data back to IsAttachment (example: DocumentType = 'StructuredData' => IsAttachment = false)
            migrationBuilder.Sql(
                @"UPDATE ""public"".""documents"" SET ""IsAttachment"" = CASE WHEN ""DocumentType"" = 'StructuredData' THEN FALSE ELSE TRUE END;"
            );

            // 3. Drop DocumentType column
            migrationBuilder.DropColumn(name: "DocumentType", schema: "public", table: "documents");
        }
    }
}
