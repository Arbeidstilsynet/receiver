using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddMeldingEntityRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meldinger_documents_MainContentId",
                schema: "receiver",
                table: "meldinger"
            );

            migrationBuilder.DropIndex(
                name: "IX_meldinger_MainContentId",
                schema: "receiver",
                table: "meldinger"
            );

            migrationBuilder.DropColumn(
                name: "MainContentId",
                schema: "receiver",
                table: "meldinger"
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsAttachment",
                schema: "receiver",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAttachment",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "MainContentId",
                schema: "receiver",
                table: "meldinger",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateIndex(
                name: "IX_meldinger_MainContentId",
                schema: "receiver",
                table: "meldinger",
                column: "MainContentId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_meldinger_documents_MainContentId",
                schema: "receiver",
                table: "meldinger",
                column: "MainContentId",
                principalSchema: "receiver",
                principalTable: "documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
