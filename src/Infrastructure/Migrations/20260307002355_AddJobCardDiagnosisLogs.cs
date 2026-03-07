using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCardDiagnosisLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "latest_diagnosis_summary",
                table: "job_cards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "latest_estimated_eta",
                table: "job_cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "latest_estimated_price",
                table: "job_cards",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_eta",
                table: "job_cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "jobcard_diagnosis_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    jobcard_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis_note = table.Column<string>(type: "text", nullable: false),
                    estimated_eta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobcard_diagnosis_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_jobcard_diagnosis_logs_job_cards_jobcard_id",
                        column: x => x.jobcard_id,
                        principalTable: "job_cards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_jobcard_diagnosis_logs_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_jobcard_diagnosis_logs_created_by_user_id",
                table: "jobcard_diagnosis_logs",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobcard_diagnosis_logs_jobcard_created",
                table: "jobcard_diagnosis_logs",
                columns: new[] { "jobcard_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jobcard_diagnosis_logs");

            migrationBuilder.DropColumn(
                name: "latest_diagnosis_summary",
                table: "job_cards");

            migrationBuilder.DropColumn(
                name: "latest_estimated_eta",
                table: "job_cards");

            migrationBuilder.DropColumn(
                name: "latest_estimated_price",
                table: "job_cards");

            migrationBuilder.DropColumn(
                name: "requested_eta",
                table: "job_cards");
        }
    }
}
