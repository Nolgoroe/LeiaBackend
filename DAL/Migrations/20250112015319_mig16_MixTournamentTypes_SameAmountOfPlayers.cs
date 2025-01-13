using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig16_MixTournamentTypes_SameAmountOfPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TournamentDataId",
                table: "PlayerTournamentSession",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentSession_TournamentDataId",
                table: "PlayerTournamentSession",
                column: "TournamentDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_TournamentsData_TournamentDataId",
                table: "PlayerTournamentSession",
                column: "TournamentDataId",
                principalTable: "TournamentsData",
                principalColumn: "TournamentDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_TournamentsData_TournamentDataId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTournamentSession_TournamentDataId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "TournamentDataId",
                table: "PlayerTournamentSession");
        }
    }
}
