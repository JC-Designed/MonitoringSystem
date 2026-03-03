using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Program",
                table: "AspNetUsers");
        }
    }
}
