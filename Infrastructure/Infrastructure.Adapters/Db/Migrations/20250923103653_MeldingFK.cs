using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class MeldingFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_meldinger_MeldingEntityId",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.DropIndex(
                name: "IX_documents_MeldingEntityId",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.DropColumn(
                name: "MeldingEntityId",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.CreateIndex(
                name: "IX_documents_MeldingId",
                schema: "receiver",
                table: "documents",
                column: "MeldingId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_documents_meldinger_MeldingId",
                schema: "receiver",
                table: "documents",
                column: "MeldingId",
                principalSchema: "receiver",
                principalTable: "meldinger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_meldinger_MeldingId",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.DropIndex(
                name: "IX_documents_MeldingId",
                schema: "receiver",
                table: "documents"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "MeldingEntityId",
                schema: "receiver",
                table: "documents",
                type: "uuid",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_documents_MeldingEntityId",
                schema: "receiver",
                table: "documents",
                column: "MeldingEntityId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_documents_meldinger_MeldingEntityId",
                schema: "receiver",
                table: "documents",
                column: "MeldingEntityId",
                principalSchema: "receiver",
                principalTable: "meldinger",
                principalColumn: "Id"
            );
        }
    }
}
