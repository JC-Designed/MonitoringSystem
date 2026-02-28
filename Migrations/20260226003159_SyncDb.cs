using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringSystem.Migrations
{
    /// <inheritdoc />
    public partial class SyncDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccounts_AccountTypes_AccountTypeTypeId",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_AccountTypeTypeId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "AccountTypeTypeId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountTypeTypeId",
                table: "UserAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UserAccounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_AccountTypeTypeId",
                table: "UserAccounts",
                column: "AccountTypeTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_AccountTypes_AccountTypeTypeId",
                table: "UserAccounts",
                column: "AccountTypeTypeId",
                principalTable: "AccountTypes",
                principalColumn: "TypeId");
        }
    }
}
