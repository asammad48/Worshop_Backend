using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommunicationLogPrintFeaturesAndRoadblockerFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_communication_logs_users_sent_by_user_id",
                table: "communication_logs");

            migrationBuilder.DropIndex(
                name: "ix_commlogs_jobcard",
                table: "communication_logs");

            migrationBuilder.DropColumn(
                name: "sent_at",
                table: "communication_logs");

            migrationBuilder.RenameColumn(
                name: "sent_by_user_id",
                table: "communication_logs",
                newName: "created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "communication_logs",
                newName: "details");

            migrationBuilder.RenameColumn(
                name: "message_type",
                table: "communication_logs",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "channel",
                table: "communication_logs",
                newName: "direction");

            migrationBuilder.RenameIndex(
                name: "IX_communication_logs_sent_by_user_id",
                table: "communication_logs",
                newName: "IX_communication_logs_created_by_user_id");

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_user_id",
                table: "roadblockers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "communication_logs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "occurred_at",
                table: "communication_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "communication_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_roadblockers_created_by_user_id",
                table: "roadblockers",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_roadblockers_resolved_by_user_id",
                table: "roadblockers",
                column: "resolved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_commlogs_jobcard_date",
                table: "communication_logs",
                columns: new[] { "job_card_id", "occurred_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_communication_logs_users_created_by_user_id",
                table: "communication_logs",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roadblockers_job_cards_jobcard_id",
                table: "roadblockers",
                column: "jobcard_id",
                principalTable: "job_cards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roadblockers_users_created_by_user_id",
                table: "roadblockers",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_roadblockers_users_resolved_by_user_id",
                table: "roadblockers",
                column: "resolved_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_communication_logs_users_created_by_user_id",
                table: "communication_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_roadblockers_job_cards_jobcard_id",
                table: "roadblockers");

            migrationBuilder.DropForeignKey(
                name: "FK_roadblockers_users_created_by_user_id",
                table: "roadblockers");

            migrationBuilder.DropForeignKey(
                name: "FK_roadblockers_users_resolved_by_user_id",
                table: "roadblockers");

            migrationBuilder.DropIndex(
                name: "IX_roadblockers_created_by_user_id",
                table: "roadblockers");

            migrationBuilder.DropIndex(
                name: "IX_roadblockers_resolved_by_user_id",
                table: "roadblockers");

            migrationBuilder.DropIndex(
                name: "ix_commlogs_jobcard_date",
                table: "communication_logs");

            migrationBuilder.DropColumn(
                name: "resolved_by_user_id",
                table: "roadblockers");

            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "communication_logs");

            migrationBuilder.DropColumn(
                name: "summary",
                table: "communication_logs");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "communication_logs",
                newName: "message_type");

            migrationBuilder.RenameColumn(
                name: "direction",
                table: "communication_logs",
                newName: "channel");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "communication_logs",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "communication_logs",
                newName: "sent_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_communication_logs_created_by_user_id",
                table: "communication_logs",
                newName: "IX_communication_logs_sent_by_user_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "communication_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sent_at",
                table: "communication_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateIndex(
                name: "ix_commlogs_jobcard",
                table: "communication_logs",
                column: "job_card_id");

            migrationBuilder.AddForeignKey(
                name: "FK_communication_logs_users_sent_by_user_id",
                table: "communication_logs",
                column: "sent_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
