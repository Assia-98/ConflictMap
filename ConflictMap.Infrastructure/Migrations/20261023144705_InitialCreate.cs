using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConflictMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conflicts",
                columns: table => new
                {
                    ConflictId = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    SideA = table.Column<string>(type: "TEXT", nullable: false),
                    SideB = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Region = table.Column<int>(type: "INTEGER", nullable: false),
                    StartYear = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conflicts", x => x.ConflictId);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GwCode = table.Column<int>(type: "INTEGER", nullable: false),
                    IsoCode = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConflictYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConflictId = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Intensity = table.Column<int>(type: "INTEGER", nullable: false),
                    BattleDeathsBest = table.Column<int>(type: "INTEGER", nullable: true),
                    BattleDeathsLow = table.Column<int>(type: "INTEGER", nullable: true),
                    BattleDeathsHigh = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictYears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConflictYears_Conflicts_ConflictId",
                        column: x => x.ConflictId,
                        principalTable: "Conflicts",
                        principalColumn: "ConflictId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConflictCountry",
                columns: table => new
                {
                    ConflictsConflictId = table.Column<int>(type: "INTEGER", nullable: false),
                    CountriesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictCountry", x => new { x.ConflictsConflictId, x.CountriesId });
                    table.ForeignKey(
                        name: "FK_ConflictCountry_Conflicts_ConflictsConflictId",
                        column: x => x.ConflictsConflictId,
                        principalTable: "Conflicts",
                        principalColumn: "ConflictId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConflictCountry_Countries_CountriesId",
                        column: x => x.CountriesId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictCountry_CountriesId",
                table: "ConflictCountry",
                column: "CountriesId");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictYears_ConflictId_Year",
                table: "ConflictYears",
                columns: new[] { "ConflictId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConflictCountry");

            migrationBuilder.DropTable(
                name: "ConflictYears");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Conflicts");
        }
    }
}
