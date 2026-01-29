using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Db.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSubscriptionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AltinnSubscription_AlternativeSubjectFilter",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_Consumer",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_Created",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_CreatedBy",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_EndPoint",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_Id",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_SourceFilter",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_SubjectFilter",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "AltinnSubscription_TypeFilter",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionEntityId",
                schema: "public",
                table: "registered_altinn_apps",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                schema: "public",
                table: "registered_altinn_apps",
                type: "integer",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "registered_subscriptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_registered_subscriptions", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "registered_default_apps",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppIdentifier = table.Column<string>(type: "text", nullable: false),
                    SubscriptionEntityId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_registered_default_apps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registered_default_apps_registered_subscriptions_Subscripti~",
                        column: x => x.SubscriptionEntityId,
                        principalSchema: "public",
                        principalTable: "registered_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_registered_altinn_apps_SubscriptionEntityId",
                schema: "public",
                table: "registered_altinn_apps",
                column: "SubscriptionEntityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_registered_default_apps_SubscriptionEntityId",
                schema: "public",
                table: "registered_default_apps",
                column: "SubscriptionEntityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_registered_subscriptions_ConsumerName",
                schema: "public",
                table: "registered_subscriptions",
                column: "ConsumerName"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_registered_altinn_apps_registered_subscriptions_Subscriptio~",
                schema: "public",
                table: "registered_altinn_apps",
                column: "SubscriptionEntityId",
                principalSchema: "public",
                principalTable: "registered_subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_registered_altinn_apps_registered_subscriptions_Subscriptio~",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropTable(name: "registered_default_apps", schema: "public");

            migrationBuilder.DropTable(name: "registered_subscriptions", schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_registered_altinn_apps_SubscriptionEntityId",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "SubscriptionEntityId",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                schema: "public",
                table: "registered_altinn_apps"
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_AlternativeSubjectFilter",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_Consumer",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "AltinnSubscription_Created",
                schema: "public",
                table: "registered_altinn_apps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_CreatedBy",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_EndPoint",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "AltinnSubscription_Id",
                schema: "public",
                table: "registered_altinn_apps",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_SourceFilter",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_SubjectFilter",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "AltinnSubscription_TypeFilter",
                schema: "public",
                table: "registered_altinn_apps",
                type: "text",
                nullable: true
            );
        }
    }
}
