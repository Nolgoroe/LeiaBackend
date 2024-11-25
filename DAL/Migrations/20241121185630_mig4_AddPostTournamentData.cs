using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig4_AddPostTournamentData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Currencies_CurrencyId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "CurrencyId",
                table: "Transactions",
                newName: "CurrenciesId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CurrencyId",
                table: "Transactions",
                newName: "IX_Transactions_CurrenciesId");

            migrationBuilder.AddColumn<int>(
                name: "CurrenciesId",
                table: "TournamentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EntryFee",
                table: "TournamentTypes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPlayers",
                table: "TournamentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DidClaim",
                table: "PlayerTournamentSession",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    RewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RewardName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrenciesId = table.Column<int>(type: "int", nullable: false),
                    RewardAmount = table.Column<int>(type: "int", nullable: true),
                    ForPosition = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.RewardId);
                    table.ForeignKey(
                        name: "FK_Rewards_Currencies_CurrenciesId",
                        column: x => x.CurrenciesId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RewardTournamentType",
                columns: table => new
                {
                    RewardId = table.Column<int>(type: "int", nullable: false),
                    TournamentTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardTournamentType", x => new { x.RewardId, x.TournamentTypeId });
                    table.ForeignKey(
                        name: "FK_RewardTournamentType_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "RewardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RewardTournamentType_TournamentTypes_TournamentTypeId",
                        column: x => x.TournamentTypeId,
                        principalTable: "TournamentTypes",
                        principalColumn: "TournamentTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentTypes_CurrenciesId",
                table: "TournamentTypes",
                column: "CurrenciesId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_CurrenciesId",
                table: "Rewards",
                column: "CurrenciesId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardTournamentType_TournamentTypeId",
                table: "RewardTournamentType",
                column: "TournamentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentTypes_Currencies_CurrenciesId",
                table: "TournamentTypes",
                column: "CurrenciesId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Currencies_CurrenciesId",
                table: "Transactions",
                column: "CurrenciesId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentTypes_Currencies_CurrenciesId",
                table: "TournamentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Currencies_CurrenciesId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "RewardTournamentType");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_TournamentTypes_CurrenciesId",
                table: "TournamentTypes");

            migrationBuilder.DropColumn(
                name: "CurrenciesId",
                table: "TournamentTypes");

            migrationBuilder.DropColumn(
                name: "EntryFee",
                table: "TournamentTypes");

            migrationBuilder.DropColumn(
                name: "NumberOfPlayers",
                table: "TournamentTypes");

            migrationBuilder.DropColumn(
                name: "DidClaim",
                table: "PlayerTournamentSession");

            migrationBuilder.RenameColumn(
                name: "CurrenciesId",
                table: "Transactions",
                newName: "CurrencyId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CurrenciesId",
                table: "Transactions",
                newName: "IX_Transactions_CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Currencies_CurrencyId",
                table: "Transactions",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
