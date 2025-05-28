using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class TransferLocalToBack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalExp",
                table: "Players",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserCode",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchievementName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementId);
                    table.ForeignKey(
                        name: "FK_Achievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyRewards",
                columns: table => new
                {
                    DailyRewardsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRewards", x => x.DailyRewardsId);
                    table.ForeignKey(
                        name: "FK_DailyRewards_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId");
                });

            migrationBuilder.CreateTable(
                name: "EggRewards",
                columns: table => new
                {
                    EggRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Count = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    RewardAmount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EggRewards", x => x.EggRewardId);
                    table.ForeignKey(
                        name: "FK_EggRewards_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId");
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.FeatureId);
                });

            migrationBuilder.CreateTable(
                name: "FTUEs",
                columns: table => new
                {
                    FtueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameTypeId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FTUEs", x => x.FtueId);
                });

            migrationBuilder.CreateTable(
                name: "GameTypeToExp",
                columns: table => new
                {
                    GameTypeToExpId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameTypeId = table.Column<int>(type: "int", nullable: false),
                    Exp = table.Column<double>(type: "float", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameTypeToExp", x => x.GameTypeToExpId);
                    table.ForeignKey(
                        name: "FK_GameTypeToExp_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId");
                    table.ForeignKey(
                        name: "FK_GameTypeToExp_GameType_GameTypeId",
                        column: x => x.GameTypeId,
                        principalTable: "GameType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameTypeToExp_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMonthlyEggs",
                columns: table => new
                {
                    ActivePlayerEggsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMonthlyEggs", x => x.ActivePlayerEggsId);
                    table.ForeignKey(
                        name: "FK_PlayerMonthlyEggs_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProfileData",
                columns: table => new
                {
                    PlayerProfileId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerPictureId = table.Column<int>(type: "int", nullable: true),
                    WinCounte = table.Column<int>(type: "int", nullable: true),
                    FavoriteGameTypeId = table.Column<int>(type: "int", nullable: true),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfileData", x => x.PlayerProfileId);
                    table.ForeignKey(
                        name: "FK_PlayerProfileData_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMainProgression",
                columns: table => new
                {
                    UserLevel = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionsDesired = table.Column<double>(type: "float", nullable: false),
                    SessionLength = table.Column<int>(type: "int", nullable: false),
                    TimePerGame = table.Column<int>(type: "int", nullable: false),
                    XpPerMinute = table.Column<double>(type: "float", nullable: false),
                    GamesPlayrd = table.Column<double>(type: "float", nullable: true),
                    XPRequired = table.Column<double>(type: "float", nullable: true),
                    XPForUnity = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMainProgression", x => x.UserLevel);
                });

            migrationBuilder.CreateTable(
                name: "AchievementElements",
                columns: table => new
                {
                    AchievementsElementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElementNameId = table.Column<int>(type: "int", nullable: false),
                    AmountNeeded = table.Column<int>(type: "int", nullable: true),
                    CurrentAmount = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AchievementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementElements", x => x.AchievementsElementId);
                    table.ForeignKey(
                        name: "FK_AchievementElements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "AchievementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDailyRewards",
                columns: table => new
                {
                    PlayerDailyRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastClaimDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentRewardDay = table.Column<int>(type: "int", nullable: false),
                    ConsecutiveDays = table.Column<int>(type: "int", nullable: false),
                    DailyRewardsId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDailyRewards", x => x.PlayerDailyRewardId);
                    table.ForeignKey(
                        name: "FK_PlayerDailyRewards_DailyRewards_DailyRewardsId",
                        column: x => x.DailyRewardsId,
                        principalTable: "DailyRewards",
                        principalColumn: "DailyRewardsId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDailyRewards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerHourlyRewards",
                columns: table => new
                {
                    HourlyRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastClaimDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DailyRewardsId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHourlyRewards", x => x.HourlyRewardId);
                    table.ForeignKey(
                        name: "FK_PlayerHourlyRewards_DailyRewards_DailyRewardsId",
                        column: x => x.DailyRewardsId,
                        principalTable: "DailyRewards",
                        principalColumn: "DailyRewardsId");
                    table.ForeignKey(
                        name: "FK_PlayerHourlyRewards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LevelRewards",
                columns: table => new
                {
                    LevelRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    RewardAmount = table.Column<double>(type: "float", nullable: false),
                    FeatureId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelRewards", x => x.LevelRewardId);
                    table.ForeignKey(
                        name: "FK_LevelRewards_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LevelRewards_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "FeatureId");
                });

            migrationBuilder.CreateTable(
                name: "PlayerFeatures",
                columns: table => new
                {
                    PlayerFeatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFeatures", x => x.PlayerFeatureId);
                    table.ForeignKey(
                        name: "FK_PlayerFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "FeatureId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFeatures_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerFtues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FtueId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFtues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerFtues_FTUEs_FtueId",
                        column: x => x.FtueId,
                        principalTable: "FTUEs",
                        principalColumn: "FtueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFtues_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GivenPlayerEggRewards",
                columns: table => new
                {
                    GivenEggRewordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivePlayerEggsId = table.Column<int>(type: "int", nullable: false),
                    EggRewardId = table.Column<int>(type: "int", nullable: false),
                    GivenDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GivenPlayerEggRewards", x => x.GivenEggRewordId);
                    table.ForeignKey(
                        name: "FK_GivenPlayerEggRewards_EggRewards_EggRewardId",
                        column: x => x.EggRewardId,
                        principalTable: "EggRewards",
                        principalColumn: "EggRewardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GivenPlayerEggRewards_PlayerMonthlyEggs_ActivePlayerEggsId",
                        column: x => x.ActivePlayerEggsId,
                        principalTable: "PlayerMonthlyEggs",
                        principalColumn: "ActivePlayerEggsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GivenPlayerLevelRewards",
                columns: table => new
                {
                    GivenLevelRewardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelRewardId = table.Column<int>(type: "int", nullable: false),
                    GivenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GivenPlayerLevelRewards", x => x.GivenLevelRewardId);
                    table.ForeignKey(
                        name: "FK_GivenPlayerLevelRewards_LevelRewards_LevelRewardId",
                        column: x => x.LevelRewardId,
                        principalTable: "LevelRewards",
                        principalColumn: "LevelRewardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GivenPlayerLevelRewards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchievementElements_AchievementId",
                table: "AchievementElements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_PlayerId",
                table: "Achievements",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRewards_CurrencyId",
                table: "DailyRewards",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EggRewards_CurrencyId",
                table: "EggRewards",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTypeToExp_CurrencyId",
                table: "GameTypeToExp",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTypeToExp_GameTypeId",
                table: "GameTypeToExp",
                column: "GameTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameTypeToExp_PlayerId",
                table: "GameTypeToExp",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerEggRewards_ActivePlayerEggsId",
                table: "GivenPlayerEggRewards",
                column: "ActivePlayerEggsId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerEggRewards_EggRewardId",
                table: "GivenPlayerEggRewards",
                column: "EggRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerLevelRewards_LevelRewardId",
                table: "GivenPlayerLevelRewards",
                column: "LevelRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_GivenPlayerLevelRewards_PlayerId",
                table: "GivenPlayerLevelRewards",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelRewards_CurrencyId",
                table: "LevelRewards",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_LevelRewards_FeatureId",
                table: "LevelRewards",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDailyRewards_DailyRewardsId",
                table: "PlayerDailyRewards",
                column: "DailyRewardsId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDailyRewards_PlayerId",
                table: "PlayerDailyRewards",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFeatures_FeatureId",
                table: "PlayerFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFeatures_PlayerId",
                table: "PlayerFeatures",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFtues_FtueId",
                table: "PlayerFtues",
                column: "FtueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFtues_PlayerId",
                table: "PlayerFtues",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHourlyRewards_DailyRewardsId",
                table: "PlayerHourlyRewards",
                column: "DailyRewardsId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHourlyRewards_PlayerId",
                table: "PlayerHourlyRewards",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMonthlyEggs_PlayerId",
                table: "PlayerMonthlyEggs",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProfileData_PlayerId",
                table: "PlayerProfileData",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementElements");

            migrationBuilder.DropTable(
                name: "GameTypeToExp");

            migrationBuilder.DropTable(
                name: "GivenPlayerEggRewards");

            migrationBuilder.DropTable(
                name: "GivenPlayerLevelRewards");

            migrationBuilder.DropTable(
                name: "PlayerDailyRewards");

            migrationBuilder.DropTable(
                name: "PlayerFeatures");

            migrationBuilder.DropTable(
                name: "PlayerFtues");

            migrationBuilder.DropTable(
                name: "PlayerHourlyRewards");

            migrationBuilder.DropTable(
                name: "PlayerProfileData");

            migrationBuilder.DropTable(
                name: "UserMainProgression");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "EggRewards");

            migrationBuilder.DropTable(
                name: "PlayerMonthlyEggs");

            migrationBuilder.DropTable(
                name: "LevelRewards");

            migrationBuilder.DropTable(
                name: "FTUEs");

            migrationBuilder.DropTable(
                name: "DailyRewards");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalExp",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserCode",
                table: "Players");
        }
    }
}
