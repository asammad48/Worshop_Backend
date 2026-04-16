using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedDriverIdInJObCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "driver_id",
                table: "job_cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_cards_driver_id",
                table: "job_cards",
                column: "driver_id");

            migrationBuilder.AddForeignKey(
                name: "FK_job_cards_drivers_driver_id",
                table: "job_cards",
                column: "driver_id",
                principalTable: "drivers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_job_cards_drivers_driver_id",
                table: "job_cards");

            migrationBuilder.DropIndex(
                name: "IX_job_cards_driver_id",
                table: "job_cards");

            migrationBuilder.DropColumn(
                name: "driver_id",
                table: "job_cards");
        }
    }
}
