using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartNagar.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Complaints",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedOfficerId",
                table: "Complaints",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Complaints",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AssignedOfficerId",
                table: "Complaints",
                column: "AssignedOfficerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_AspNetUsers_AssignedOfficerId",
                table: "Complaints",
                column: "AssignedOfficerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_AspNetUsers_AssignedOfficerId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_AssignedOfficerId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Complaints");
        }
    }
}
