using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                schema: "receiver",
                table: "meldinger"
            );

            migrationBuilder.DropColumn(name: "ContentId", schema: "receiver", table: "meldinger");

            migrationBuilder.AddColumn<Guid>(
                name: "MainContentId",
                schema: "receiver",
                table: "meldinger",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "receiver",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalDocumentReference = table.Column<string>(type: "text", nullable: false),
                    MeldingEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_documents_meldinger_MeldingEntityId",
                        column: x => x.MeldingEntityId,
                        principalSchema: "receiver",
                        principalTable: "meldinger",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_meldinger_MainContentId",
                schema: "receiver",
                table: "meldinger",
                column: "MainContentId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_documents_MeldingEntityId",
                schema: "receiver",
                table: "documents",
                column: "MeldingEntityId"
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meldinger_documents_MainContentId",
                schema: "receiver",
                table: "meldinger"
            );

            migrationBuilder.DropTable(name: "documents", schema: "receiver");

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

            migrationBuilder.AddColumn<List<string>>(
                name: "AttachmentIds",
                schema: "receiver",
                table: "meldinger",
                type: "text[]",
                nullable: false
            );

            migrationBuilder.AddColumn<string>(
                name: "ContentId",
                schema: "receiver",
                table: "meldinger",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
        }
    }
}
