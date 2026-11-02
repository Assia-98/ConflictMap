using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConflictMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveSidesToConflictYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SideA",
                table: "Conflicts");

            migrationBuilder.DropColumn(
                name: "SideB",
                table: "Conflicts");

            migrationBuilder.AddColumn<string>(
                name: "SideA",
                table: "ConflictYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SideB",
                table: "ConflictYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SideA",
                table: "ConflictYears");

            migrationBuilder.DropColumn(
                name: "SideB",
                table: "ConflictYears");

            migrationBuilder.AddColumn<string>(
                name: "SideA",
                table: "Conflicts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SideB",
                table: "Conflicts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
