using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig12matchqueuetodb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerActiveTournaments_PlayerId_TournamentId",
                table: "PlayerActiveTournaments");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PlayerActiveTournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "EntryFee",
                table: "PlayerActiveTournaments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TournamentTypeId",
                table: "PlayerActiveTournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_CurrencyId",
                table: "PlayerActiveTournaments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_MatchmakeStartTime",
                table: "PlayerActiveTournaments",
                column: "MatchmakeStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_TournamentId",
                table: "PlayerActiveTournaments",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_TournamentTypeId",
                table: "PlayerActiveTournaments",
                column: "TournamentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerActiveTournaments_Currencies_CurrencyId",
                table: "PlayerActiveTournaments",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerActiveTournaments_TournamentTypes_TournamentTypeId",
                table: "PlayerActiveTournaments",
                column: "TournamentTypeId",
                principalTable: "TournamentTypes",
                principalColumn: "TournamentTypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerActiveTournaments_Currencies_CurrencyId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerActiveTournaments_TournamentTypes_TournamentTypeId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerActiveTournaments_CurrencyId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerActiveTournaments_MatchmakeStartTime",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerActiveTournaments_TournamentId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerActiveTournaments_TournamentTypeId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropColumn(
                name: "EntryFee",
                table: "PlayerActiveTournaments");

            migrationBuilder.DropColumn(
                name: "TournamentTypeId",
                table: "PlayerActiveTournaments");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_PlayerId_TournamentId",
                table: "PlayerActiveTournaments",
                columns: new[] { "PlayerId", "TournamentId" },
                unique: true);
        }
    }
}
