using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartNagar.Migrations
{
    /// <inheritdoc />
    public partial class AddGarbageReminderEmailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "GarbageReminders",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAtUtc",
                table: "GarbageReminders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailSent",
                table: "GarbageReminders",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderDateTimeUtc",
                table: "GarbageReminders",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_GarbageReminders_CitizenId",
                table: "GarbageReminders",
                column: "CitizenId");

            migrationBuilder.AddForeignKey(
                name: "FK_GarbageReminders_AspNetUsers_CitizenId",
                table: "GarbageReminders",
                column: "CitizenId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GarbageReminders_AspNetUsers_CitizenId",
                table: "GarbageReminders");

            migrationBuilder.DropIndex(
                name: "IX_GarbageReminders_CitizenId",
                table: "GarbageReminders");

            migrationBuilder.DropColumn(
                name: "EmailSentAtUtc",
                table: "GarbageReminders");

            migrationBuilder.DropColumn(
                name: "IsEmailSent",
                table: "GarbageReminders");

            migrationBuilder.DropColumn(
                name: "ReminderDateTimeUtc",
                table: "GarbageReminders");

            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "GarbageReminders",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
