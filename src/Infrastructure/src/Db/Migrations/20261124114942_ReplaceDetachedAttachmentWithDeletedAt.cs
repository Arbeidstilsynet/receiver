using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDetachedAttachmentWithDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "public",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true
            );

            // Deleted attachments were previously stored as DocumentType 'DetachedAttachment'.
            // UpdatedAt is the best available approximation of when they were deleted.
            migrationBuilder.Sql(
                @"UPDATE ""public"".""documents""
                  SET ""DocumentType"" = 'Attachment', ""DeletedAt"" = ""UpdatedAt""
                  WHERE ""DocumentType"" = 'DetachedAttachment';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""public"".""documents""
                  SET ""DocumentType"" = 'DetachedAttachment'
                  WHERE ""DeletedAt"" IS NOT NULL;"
            );

            migrationBuilder.DropColumn(name: "DeletedAt", schema: "public", table: "documents");
        }
    }
}
