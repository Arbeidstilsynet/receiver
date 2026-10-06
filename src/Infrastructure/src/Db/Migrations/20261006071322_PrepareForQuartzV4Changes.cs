using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Db.Migrations
{
    /// <inheritdoc />
    public partial class PrepareForQuartzV4Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_next_fire_time",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_state",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_j_req_recovery",
                schema: "quartz",
                table: "qrtz_job_details");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_job_group",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_job_name",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_job_req_recovery",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_trig_group",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_trig_inst_name",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_trig_name",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.RenameIndex(
                name: "IX_qrtz_triggers_sched_name_job_name_job_group",
                schema: "quartz",
                table: "qrtz_triggers",
                newName: "idx_qrtz_t_j");

            migrationBuilder.RenameIndex(
                name: "idx_qrtz_ft_trig_nm_gp",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                newName: "idx_qrtz_ft_t_g");

            migrationBuilder.AddColumn<int>(
                name: "continuation_condition",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "continues_trigger_group",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "continues_trigger_name",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "execution_group",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "overlap_policy",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pause_reason",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "varchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "paused_at",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "paused_by",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_node",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "preferred_node_auto",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "retry_attempt",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retry_policy",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "varchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pause_reason",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps",
                type: "varchar(250)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "paused_at",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "paused_by",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "execution_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                type: "varchar(200)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "progress",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "progress_message",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                type: "varchar(250)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "qrtz_execution_history",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    entry_id = table.Column<string>(type: "text", nullable: false),
                    instance_name = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    fired_time = table.Column<long>(type: "bigint", nullable: false),
                    run_time = table.Column<long>(type: "bigint", nullable: false),
                    succeeded = table.Column<bool>(type: "bool", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_attempt = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    retry_scheduled = table.Column<bool>(type: "bool", nullable: false, defaultValue: false),
                    execution_log = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qrtz_execution_history", x => new { x.sched_name, x.entry_id });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_misfire_history",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    entry_id = table.Column<string>(type: "text", nullable: false),
                    instance_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: true),
                    job_group = table.Column<string>(type: "text", nullable: true),
                    misfire_time = table.Column<long>(type: "bigint", nullable: false),
                    sched_time = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qrtz_misfire_history", x => new { x.sched_name, x.entry_id });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_paused_job_grps",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    pause_reason = table.Column<string>(type: "varchar(250)", nullable: true),
                    paused_by = table.Column<string>(type: "varchar(200)", nullable: true),
                    paused_at = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qrtz_paused_job_grps", x => new { x.sched_name, x.job_group });
                });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_c",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "sched_name", "calendar_name" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_g_n",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "sched_name", "trigger_group", "trigger_name" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "sched_name", "trigger_state", "next_fire_time", "priority", "misfire_instr" },
                descending: new[] { false, false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_j_g_n",
                schema: "quartz",
                table: "qrtz_job_details",
                columns: new[] { "sched_name", "job_group", "job_name" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_inst_job_req_rcvry",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                columns: new[] { "sched_name", "instance_name", "requests_recovery" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_j_g",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                columns: new[] { "sched_name", "job_name", "job_group" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_eh_fired_time",
                schema: "quartz",
                table: "qrtz_execution_history",
                columns: new[] { "sched_name", "fired_time" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_eh_inst",
                schema: "quartz",
                table: "qrtz_execution_history",
                columns: new[] { "sched_name", "instance_name" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_mh_inst",
                schema: "quartz",
                table: "qrtz_misfire_history",
                columns: new[] { "sched_name", "instance_name" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_mh_misfire_time",
                schema: "quartz",
                table: "qrtz_misfire_history",
                columns: new[] { "sched_name", "misfire_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "qrtz_execution_history",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_misfire_history",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_paused_job_grps",
                schema: "quartz");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_c",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_g_n",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_j_g_n",
                schema: "quartz",
                table: "qrtz_job_details");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_inst_job_req_rcvry",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropIndex(
                name: "idx_qrtz_ft_j_g",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropColumn(
                name: "continuation_condition",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "continues_trigger_group",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "continues_trigger_name",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "execution_group",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "overlap_policy",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "pause_reason",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "paused_at",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "paused_by",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "preferred_node",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "preferred_node_auto",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "retry_attempt",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "retry_policy",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.DropColumn(
                name: "pause_reason",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps");

            migrationBuilder.DropColumn(
                name: "paused_at",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps");

            migrationBuilder.DropColumn(
                name: "paused_by",
                schema: "quartz",
                table: "qrtz_paused_trigger_grps");

            migrationBuilder.DropColumn(
                name: "execution_group",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropColumn(
                name: "progress",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.DropColumn(
                name: "progress_message",
                schema: "quartz",
                table: "qrtz_fired_triggers");

            migrationBuilder.RenameIndex(
                name: "idx_qrtz_t_j",
                schema: "quartz",
                table: "qrtz_triggers",
                newName: "IX_qrtz_triggers_sched_name_job_name_job_group");

            migrationBuilder.RenameIndex(
                name: "idx_qrtz_ft_t_g",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                newName: "idx_qrtz_ft_trig_nm_gp");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_next_fire_time",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "next_fire_time");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "next_fire_time", "trigger_state" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_state",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "trigger_state");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_j_req_recovery",
                schema: "quartz",
                table: "qrtz_job_details",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_req_recovery",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_inst_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "instance_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_name");
        }
    }
}
