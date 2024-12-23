using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig9playeractivetournamentandbackendlog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackendLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Log = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackendLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerActiveTournaments",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    MatchmakeStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoinTournamentTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerActiveTournaments", x => x.PlayerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackendLogs_Timestamp",
                table: "BackendLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_PlayerId",
                table: "PlayerActiveTournaments",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActiveTournaments_PlayerId_TournamentId",
                table: "PlayerActiveTournaments",
                columns: new[] { "PlayerId", "TournamentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackendLogs");

            migrationBuilder.DropTable(
                name: "PlayerActiveTournaments");
        }
    }
}
