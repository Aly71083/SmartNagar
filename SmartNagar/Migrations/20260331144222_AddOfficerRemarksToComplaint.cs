using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartNagar.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerRemarksToComplaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OfficerRemarks",
                table: "Complaints",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficerRemarks",
                table: "Complaints");
        }
    }
}
