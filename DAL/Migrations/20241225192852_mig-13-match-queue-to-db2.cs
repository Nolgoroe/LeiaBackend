using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig13matchqueuetodb2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "IX_PlayerActiveTournaments_TournamentTypeId",
                table: "PlayerActiveTournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_CurrencyId",
                table: "PlayerActiveTournaments",
                column: "CurrencyId");

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
    }
}
