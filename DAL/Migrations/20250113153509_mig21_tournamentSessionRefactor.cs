using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig21_tournamentSessionRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_Sessions_SessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTournamentSession_SessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "PlayerTournamentSession");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "PlayerTournamentSession",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentSession_SessionId",
                table: "PlayerTournamentSession",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_Sessions_SessionId",
                table: "PlayerTournamentSession",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId");
        }
    }
}
