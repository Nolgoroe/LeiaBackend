using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeObgectsManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AchievementElements_Achievements_AchievementId",
                table: "AchievementElements");

            migrationBuilder.DropIndex(
                name: "IX_AchievementElements_AchievementId",
                table: "AchievementElements");

            migrationBuilder.DropColumn(
                name: "AchievementId",
                table: "AchievementElements");

            migrationBuilder.AddColumn<double>(
                name: "OpenForTime",
                table: "TournamentTypes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OpenForTime",
                table: "Features",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriesObjects",
                columns: table => new
                {
                    CategoryObjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesObjects", x => x.CategoryObjectId);
                });

            migrationBuilder.CreateTable(
                name: "GivenPlayerAchievements",
                columns: table => new
                {
                    GivenAchievementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchievementId = table.Column<int>(type: "int", nullable: false),
                    AchievementsElementId = table.Column<int>(type: "int", nullable: false),
                    GivenDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GivenPlayerAchievements", x => x.GivenAchievementId);
                    table.ForeignKey(
                        name: "FK_GivenPlayerAchievements_AchievementElements_AchievementsElementId",
                        column: x => x.AchievementsElementId,
                        principalTable: "AchievementElements",
                        principalColumn: "AchievementsElementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GivenPlayerAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "AchievementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BonusAmountUSD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Gems = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpenForTime = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTimeManager",
                columns: table => new
                {
                    TimeManagerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryObjectId = table.Column<int>(type: "int", nullable: false),
                    TimeObjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTimeManager", x => x.TimeManagerId);
                    table.ForeignKey(
                        name: "FK_PlayerTimeManager_CategoriesObjects_CategoryObjectId",
                        column: x => x.CategoryObjectId,
                        principalTable: "CategoriesObjects",
                        principalColumn: "CategoryObjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTimeManager_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerAchievements_AchievementId",
                table: "GivenPlayerAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerAchievements_AchievementsElementId",
                table: "GivenPlayerAchievements",
                column: "AchievementsElementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTimeManager_CategoryObjectId",
                table: "PlayerTimeManager",
                column: "CategoryObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTimeManager_PlayerId",
                table: "PlayerTimeManager",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GivenPlayerAchievements");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "PlayerTimeManager");

            migrationBuilder.DropTable(
                name: "CategoriesObjects");

            migrationBuilder.DropColumn(
                name: "OpenForTime",
                table: "TournamentTypes");

            migrationBuilder.DropColumn(
                name: "OpenForTime",
                table: "Features");

            migrationBuilder.AddColumn<int>(
                name: "AchievementId",
                table: "AchievementElements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AchievementElements_AchievementId",
                table: "AchievementElements",
                column: "AchievementId");

            migrationBuilder.AddForeignKey(
                name: "FK_AchievementElements_Achievements_AchievementId",
                table: "AchievementElements",
                column: "AchievementId",
                principalTable: "Achievements",
                principalColumn: "AchievementId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
