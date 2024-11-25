using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig5_TournamentTypeCurrenciesIdNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentTypes_Currencies_CurrenciesId",
                table: "TournamentTypes");

            migrationBuilder.AlterColumn<int>(
                name: "CurrenciesId",
                table: "TournamentTypes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentTypes_Currencies_CurrenciesId",
                table: "TournamentTypes",
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

            migrationBuilder.AlterColumn<int>(
                name: "CurrenciesId",
                table: "TournamentTypes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentTypes_Currencies_CurrenciesId",
                table: "TournamentTypes",
                column: "CurrenciesId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId");
        }
    }
}
