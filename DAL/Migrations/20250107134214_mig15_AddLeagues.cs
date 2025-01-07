using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig15_AddLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Rewards",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeagueId",
                table: "Players",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "League",
                columns: table => new
                {
                    LeagueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_League", x => x.LeagueId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_LeagueId",
                table: "Rewards",
                column: "LeagueId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_LeagueId",
                table: "Players",
                column: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_League_LeagueId",
                table: "Players",
                column: "LeagueId",
                principalTable: "League",
                principalColumn: "LeagueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rewards_League_LeagueId",
                table: "Rewards",
                column: "LeagueId",
                principalTable: "League",
                principalColumn: "LeagueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_League_LeagueId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_League_LeagueId",
                table: "Rewards");

            migrationBuilder.DropTable(
                name: "League");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_LeagueId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_Players_LeagueId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "LeagueId",
                table: "Players");
        }
    }
}
