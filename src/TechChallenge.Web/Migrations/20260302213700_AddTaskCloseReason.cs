using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechChallenge.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCloseReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFastClosed",
                table: "Tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsFastClosed",
                table: "Tasks");
        }
    }
}
