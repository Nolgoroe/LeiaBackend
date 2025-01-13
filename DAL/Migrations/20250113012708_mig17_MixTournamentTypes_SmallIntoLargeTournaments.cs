using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig17_MixTournamentTypes_SmallIntoLargeTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTournamentId",
                table: "Tournaments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_ParentTournamentId",
                table: "Tournaments",
                column: "ParentTournamentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Tournaments_ParentTournamentId",
                table: "Tournaments",
                column: "ParentTournamentId",
                principalTable: "Tournaments",
                principalColumn: "TournamentSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Tournaments_ParentTournamentId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_ParentTournamentId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "ParentTournamentId",
                table: "Tournaments");
        }
    }
}
