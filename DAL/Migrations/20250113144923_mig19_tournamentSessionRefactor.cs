using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig19_tournamentSessionRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_TournamentsData_TournamentDataId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_TournamentsData_TournamentDataId",
                table: "Tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Tournaments_ParentTournamentId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "SessionDataTournamentSession");

            migrationBuilder.DropTable(
                name: "TournamentsData");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_TournamentDataId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "TournamentDataId",
                table: "Tournaments");

            migrationBuilder.RenameColumn(
                name: "ParentTournamentId",
                table: "Tournaments",
                newName: "SessionDataSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Tournaments_ParentTournamentId",
                table: "Tournaments",
                newName: "IX_Tournaments_SessionDataSessionId");

            ///<summary>
            /// All commented out code below was removed so the migrations could be applied successfully.
            /// Because next migrations canceled the changes made in this migration. Thus applying this migration would fail.
            ///</summary>
            
           /* migrationBuilder.RenameColumn(
                name: "TournamentDataId",
                table: "PlayerTournamentSession",
                newName: "TournamentSessionId1");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTournamentSession_TournamentDataId",
                table: "PlayerTournamentSession",
                newName: "IX_PlayerTournamentSession_TournamentSessionId1");*/

            migrationBuilder.AddColumn<DateTime>(
                name: "Endtime",
                table: "Tournaments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Tournaments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "ForPosition",
                table: "Rewards",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinTime",
                table: "PlayerTournamentSession",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "PlayerTournamentSession",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "PlayerTournamentSession",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmitScoreTime",
                table: "PlayerTournamentSession",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TournamentTypeId",
                table: "PlayerTournamentSession",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentSession_SessionId",
                table: "PlayerTournamentSession",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTournamentSession_TournamentTypeId",
                table: "PlayerTournamentSession",
                column: "TournamentTypeId");

         /*   migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_Sessions_SessionId",
                table: "PlayerTournamentSession",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId");*/

          /*  migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_TournamentTypes_TournamentTypeId",
                table: "PlayerTournamentSession",
                column: "TournamentTypeId",
                principalTable: "TournamentTypes",
                principalColumn: "TournamentTypeId",
                onDelete: ReferentialAction.NoAction);*/

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId",
                table: "PlayerTournamentSession",
                column: "TournamentSessionId",
                principalTable: "Tournaments",
                principalColumn: "TournamentSessionId");

          /* migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId1",
                table: "PlayerTournamentSession",
                column: "TournamentSessionId1",
                principalTable: "Tournaments",
                principalColumn: "TournamentSessionId");
          */

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Sessions_SessionDataSessionId",
                table: "Tournaments",
                column: "SessionDataSessionId",
                principalTable: "Sessions",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_Sessions_SessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_TournamentTypes_TournamentTypeId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId",
                table: "PlayerTournamentSession");

           /* migrationBuilder.DropForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId1",
                table: "PlayerTournamentSession");*/

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Sessions_SessionDataSessionId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTournamentSession_SessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropIndex(
                name: "IX_PlayerTournamentSession_TournamentTypeId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "Endtime",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "JoinTime",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "SubmitScoreTime",
                table: "PlayerTournamentSession");

            migrationBuilder.DropColumn(
                name: "TournamentTypeId",
                table: "PlayerTournamentSession");

            migrationBuilder.RenameColumn(
                name: "SessionDataSessionId",
                table: "Tournaments",
                newName: "ParentTournamentId");

            migrationBuilder.RenameIndex(
                name: "IX_Tournaments_SessionDataSessionId",
                table: "Tournaments",
                newName: "IX_Tournaments_ParentTournamentId");

            /*migrationBuilder.RenameColumn(
                name: "TournamentSessionId1",
                table: "PlayerTournamentSession",
                newName: "TournamentDataId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerTournamentSession_TournamentSessionId1",
                table: "PlayerTournamentSession",
                newName: "IX_PlayerTournamentSession_TournamentDataId");*/

            migrationBuilder.AddColumn<int>(
                name: "TournamentDataId",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ForPosition",
                table: "Rewards",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "SessionDataTournamentSession",
                columns: table => new
                {
                    SessionsSessionId = table.Column<int>(type: "int", nullable: false),
                    TournamentSessionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionDataTournamentSession", x => new { x.SessionsSessionId, x.TournamentSessionId });
                    table.ForeignKey(
                        name: "FK_SessionDataTournamentSession_Sessions_SessionsSessionId",
                        column: x => x.SessionsSessionId,
                        principalTable: "Sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionDataTournamentSession_Tournaments_TournamentSessionId",
                        column: x => x.TournamentSessionId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentsData",
                columns: table => new
                {
                    TournamentDataId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EarningCurrencyId = table.Column<int>(type: "int", nullable: false),
                    EntryFeeCurrencyId = table.Column<int>(type: "int", nullable: false),
                    TournamentTypeId = table.Column<int>(type: "int", nullable: false),
                    Earning = table.Column<double>(type: "float", nullable: false),
                    EntryFee = table.Column<double>(type: "float", nullable: false),
                    NumBoosterClicked = table.Column<int>(type: "int", nullable: true),
                    NumPowerUps = table.Column<int>(type: "int", nullable: true),
                    TournamentEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TournamentStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentsData", x => x.TournamentDataId);
                    table.ForeignKey(
                        name: "FK_TournamentsData_Currencies_EarningCurrencyId",
                        column: x => x.EarningCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentsData_Currencies_EntryFeeCurrencyId",
                        column: x => x.EntryFeeCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentsData_TournamentTypes_TournamentTypeId",
                        column: x => x.TournamentTypeId,
                        principalTable: "TournamentTypes",
                        principalColumn: "TournamentTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_TournamentDataId",
                table: "Tournaments",
                column: "TournamentDataId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionDataTournamentSession_TournamentSessionId",
                table: "SessionDataTournamentSession",
                column: "TournamentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentsData_EarningCurrencyId",
                table: "TournamentsData",
                column: "EarningCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentsData_EntryFeeCurrencyId",
                table: "TournamentsData",
                column: "EntryFeeCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentsData_TournamentTypeId",
                table: "TournamentsData",
                column: "TournamentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_TournamentsData_TournamentDataId",
                table: "PlayerTournamentSession",
                column: "TournamentDataId",
                principalTable: "TournamentsData",
                principalColumn: "TournamentDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerTournamentSession_Tournaments_TournamentSessionId",
                table: "PlayerTournamentSession",
                column: "TournamentSessionId",
                principalTable: "Tournaments",
                principalColumn: "TournamentSessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_TournamentsData_TournamentDataId",
                table: "Tournaments",
                column: "TournamentDataId",
                principalTable: "TournamentsData",
                principalColumn: "TournamentDataId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tournaments_Tournaments_ParentTournamentId",
                table: "Tournaments",
                column: "ParentTournamentId",
                principalTable: "Tournaments",
                principalColumn: "TournamentSessionId");
        }
    }
}
