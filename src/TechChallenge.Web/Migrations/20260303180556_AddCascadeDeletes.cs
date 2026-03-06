using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechChallenge.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TeamMembers_AssigneeId",
                table: "Tasks");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TeamMembers_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TeamMembers_AssigneeId",
                table: "Tasks");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TeamMembers_AssigneeId",
                table: "Tasks",
                column: "AssigneeId",
                principalTable: "TeamMembers",
                principalColumn: "Id");
        }
    }
}
