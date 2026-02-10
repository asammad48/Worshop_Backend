using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTaskReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "jobtask_id",
                table: "jobcard_time_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_jobcard_time_logs_jobtask_id",
                table: "jobcard_time_logs",
                column: "jobtask_id");

            migrationBuilder.AddForeignKey(
                name: "FK_jobcard_time_logs_job_tasks_jobtask_id",
                table: "jobcard_time_logs",
                column: "jobtask_id",
                principalTable: "job_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_jobcard_time_logs_job_tasks_jobtask_id",
                table: "jobcard_time_logs");

            migrationBuilder.DropIndex(
                name: "IX_jobcard_time_logs_jobtask_id",
                table: "jobcard_time_logs");

            migrationBuilder.DropColumn(
                name: "jobtask_id",
                table: "jobcard_time_logs");
        }
    }
}
