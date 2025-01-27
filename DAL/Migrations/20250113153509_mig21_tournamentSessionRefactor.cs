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
            ///<summary>
            /// All commented out code below was removed so the migrations could be applied successfully.
            /// Because next migrations canceled the changes made in this migration. Thus applying this migration would fail.
            ///</summary>
           
            /*        migrationBuilder.DropForeignKey(
                        name: "FK_PlayerTournamentSession_Sessions_SessionId",
                        table: "PlayerTournamentSession");*/

            /*   migrationBuilder.DropIndex(
                   name: "IX_PlayerTournamentSession_SessionId",
                   table: "PlayerTournamentSession");*/
            /*
                        migrationBuilder.DropColumn(
                            name: "SessionId",
                            table: "PlayerTournamentSession");*/
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
