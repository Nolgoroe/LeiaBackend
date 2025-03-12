using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class mig31gametype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameTypeId",
                table: "Tournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GameTypeId",
                table: "PlayerActiveTournaments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameType", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GameType",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "Object Match" });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_GameTypeId",
                table: "Tournaments",
                column: "GameTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameType");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_GameTypeId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "GameTypeId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "GameTypeId",
                table: "PlayerActiveTournaments");
        }
    }
}
