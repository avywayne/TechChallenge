using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechChallenge.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogEntityType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Tasks_ParentId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ParentId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Tasks");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ActivityLogs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "ActivityLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "ActivityLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Tasks_ParentTaskId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "ActivityLogs");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "ActivityLogs",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentId",
                table: "Tasks",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Tasks_ParentId",
                table: "Tasks",
                column: "ParentId",
                principalTable: "Tasks",
                principalColumn: "Id");
        }
    }
}
