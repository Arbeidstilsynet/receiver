using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "fixed_delay",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "boolean",
                nullable: true
            );

            migrationBuilder.AddColumn<long>(
                name: "misfire_threshold",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "bigint",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "quartz_version",
                schema: "quartz",
                table: "qrtz_scheduler_state",
                type: "varchar(20)",
                nullable: true
            );

            migrationBuilder.AddColumn<long>(
                name: "interrupt_requested_time",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                type: "bigint",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "fire_instance_id",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "job_input",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "job_input_too_large",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "bool",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "manual",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "bool",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "metrics",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<int>(
                name: "result",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "summary",
                schema: "quartz",
                table: "qrtz_execution_history",
                type: "text",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "qrtz_job_status",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    first_fired_time = table.Column<long>(type: "bigint", nullable: false),
                    last_fired_time = table.Column<long>(type: "bigint", nullable: false),
                    last_result = table.Column<int>(type: "integer", nullable: false),
                    last_run_time = table.Column<long>(type: "bigint", nullable: false),
                    last_instance_name = table.Column<string>(type: "text", nullable: false),
                    last_entry_id = table.Column<string>(type: "text", nullable: true),
                    last_summary = table.Column<string>(type: "text", nullable: true),
                    last_success_time = table.Column<long>(type: "bigint", nullable: true),
                    last_failure_time = table.Column<long>(type: "bigint", nullable: true),
                    last_failure_message = table.Column<string>(type: "text", nullable: true),
                    consecutive_failures = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    run_count = table.Column<long>(
                        type: "bigint",
                        nullable: false,
                        defaultValue: 0L
                    ),
                    failure_count = table.Column<long>(
                        type: "bigint",
                        nullable: false,
                        defaultValue: 0L
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qrtz_job_status", x => new
                    {
                        x.sched_name,
                        x.job_group,
                        x.job_name,
                    });
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "qrtz_job_status", schema: "quartz");

            migrationBuilder.DropColumn(
                name: "fixed_delay",
                schema: "quartz",
                table: "qrtz_triggers"
            );

            migrationBuilder.DropColumn(
                name: "misfire_threshold",
                schema: "quartz",
                table: "qrtz_triggers"
            );

            migrationBuilder.DropColumn(
                name: "quartz_version",
                schema: "quartz",
                table: "qrtz_scheduler_state"
            );

            migrationBuilder.DropColumn(
                name: "interrupt_requested_time",
                schema: "quartz",
                table: "qrtz_fired_triggers"
            );

            migrationBuilder.DropColumn(
                name: "fire_instance_id",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "job_input",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "job_input_too_large",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "manual",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "metrics",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "result",
                schema: "quartz",
                table: "qrtz_execution_history"
            );

            migrationBuilder.DropColumn(
                name: "summary",
                schema: "quartz",
                table: "qrtz_execution_history"
            );
        }
    }
}
