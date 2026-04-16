using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartNagar.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOtpFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailOtp",
                table: "AspNetUsers",
                type: "varchar(6)",
                maxLength: 6,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailOtpExpiryUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAtUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailOtp",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailOtpExpiryUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAtUtc",
                table: "AspNetUsers");
        }
    }
}
