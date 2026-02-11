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
            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                schema: "public",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            // 2. Migrate IsAttachment data to DocumentType with logic. MainDocument will need to be moved manually
            migrationBuilder.Sql(
                @"
                UPDATE ""public"".""documents"" d
                SET ""DocumentType"" =
                    CASE
                        WHEN d.""IsAttachment"" = FALSE THEN 2 -- StructuredData
                        ELSE 0 -- Attachment 
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

            // 2. Migrate DocumentType data back to IsAttachment (example: DocumentType = 2 => IsAttachment = true)
            migrationBuilder.Sql(
                @"UPDATE ""public"".""documents"" SET ""IsAttachment"" = CASE WHEN ""DocumentType"" = 2 THEN TRUE ELSE FALSE END;"
            );

            // 3. Drop DocumentType column
            migrationBuilder.DropColumn(name: "DocumentType", schema: "public", table: "documents");
        }
    }
}
