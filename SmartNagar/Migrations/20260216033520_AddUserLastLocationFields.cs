using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartNagar.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLastLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AddColumn<double>(
                name: "LastLat",
                table: "AspNetUsers",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastLng",
                table: "AspNetUsers",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationAt",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "LastLat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLng",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLocationAt",
                table: "AspNetUsers");
        }
    }
}
