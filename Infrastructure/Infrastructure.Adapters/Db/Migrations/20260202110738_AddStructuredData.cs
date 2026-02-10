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

            // 2. Migrate IsAttachment data to DocumentType with logic based on ApplicationId and FileName
            migrationBuilder.Sql(
                @"
                UPDATE ""public"".""documents"" d
                SET ""DocumentType"" =
                    CASE
                        WHEN d.""IsAttachment"" = FALSE THEN 2 -- StructuredData
                        WHEN d.""IsAttachment"" = TRUE THEN
                            CASE
                                WHEN m.""ApplicationId"" = 'ulykkesvarsel' AND d.""FileName"" = 'Varsel om arbeidsulykke med alvorlig personskade eller dødsfall.pdf' THEN 1 -- MainContent
                                ELSE 0 -- Attachment
                            END
                        ELSE 0 -- fallback to Attachment if IsAttachment is NULL (shouldn't happen due to default, but just in case)
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

            // 2. Migrate DocumentType data back to IsAttachment (example: DocumentType = 1 => IsAttachment = true)
            migrationBuilder.Sql(
                @"UPDATE ""public"".""documents"" SET ""IsAttachment"" = CASE WHEN ""DocumentType"" = 1 THEN TRUE ELSE FALSE END;"
            );

            // 3. Drop DocumentType column
            migrationBuilder.DropColumn(name: "DocumentType", schema: "public", table: "documents");
        }
    }
}
