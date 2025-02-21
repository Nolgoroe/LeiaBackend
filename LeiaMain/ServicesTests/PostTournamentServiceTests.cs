using Xunit;
using Glicko2;
using Services;
using DataObjects;

namespace ServiceTests
{
    public class PostTournamnetServiceTests
    {

        [Fact]
        public void TestCalculatePlayerRating2Player()
        {
            var service = new PostTournamentService();
            var startRating = 1500;
            var maxRatingChange = 100; // amount of maximum rating drift per each player
            var tournamentSession = new TournamentSession();
            var player1 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
                Name = "p1",
            };
            var playerSession1 = new PlayerTournamentSession()
            {
                PlayerId = player1.PlayerId,
                PlayerScore = 100,
                TournamentTypeId = 2,
            };
            var player2 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
                Name = "p2",
            };
            var playerSession2 = new PlayerTournamentSession()
            {
                PlayerId = player2.PlayerId,
                PlayerScore = 180,
                TournamentTypeId = 2,
            };
           
            var allTournamentTypes = new[]
            {
                new TournamentType()
                {
                    TournamentTypeId = 2,
                    NumberOfPlayers = 3,
                }
            };
            Assert.NotEqual(player1.PlayerId, player2.PlayerId);
            tournamentSession.PlayerTournamentSessions.Add(playerSession1);
            tournamentSession.PlayerTournamentSessions.Add(playerSession2);
            tournamentSession.Players.Add(player1);
            tournamentSession.Players.Add(player2);
            var ratingResults = service.CalculatePlayersRatingFromTournament(tournamentSession, allTournamentTypes);
            var player1NewRating = ratingResults[player1.PlayerId];
            var player2NewRating = ratingResults[player2.PlayerId];
            Assert.True(player1NewRating < startRating);
            Assert.True(player2NewRating > startRating);
            Assert.True(player1NewRating < player2NewRating, "Loser got greater rating than winner");
            Assert.True(player1NewRating > startRating - maxRatingChange);
            Assert.True(player2NewRating < startRating + maxRatingChange);
        }


        [Fact]
        public void TestCalculatePlayerRating3Player()
        {
            var service = new PostTournamentService();
            var startRating = 1500;
            var maxRatingChange = 100; // amount of maximum rating drift per each player
            var tournamentSession = new TournamentSession();
            var player1 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
                Name = "p1",
            };
            var playerSession1 = new PlayerTournamentSession()
            {
                PlayerId = player1.PlayerId,
                PlayerScore = 100,
                TournamentTypeId = 2,
            };
            var player2 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
                Name = "p2",
            };
            var playerSession2 = new PlayerTournamentSession()
            {
                PlayerId = player2.PlayerId,
                PlayerScore = 180,
                TournamentTypeId = 2,
            };
            var player3 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
                Name = "p3",
            };
            var playerSession3 = new PlayerTournamentSession()
            {
                PlayerId = player3.PlayerId,
                PlayerScore = 200,
                TournamentTypeId = 2,
            };
            var allTournamentTypes = new[]
            {
                new TournamentType()
                {
                    TournamentTypeId = 2,
                    NumberOfPlayers = 3,
                }
            };
            Assert.NotEqual(player1.PlayerId, player2.PlayerId);
            tournamentSession.PlayerTournamentSessions.Add(playerSession1);
            tournamentSession.PlayerTournamentSessions.Add(playerSession2);
            tournamentSession.PlayerTournamentSessions.Add(playerSession3);
            tournamentSession.Players.Add(player1);
            tournamentSession.Players.Add(player2);
            tournamentSession.Players.Add(player3);
            var ratingResults = service.CalculatePlayersRatingFromTournament(tournamentSession, allTournamentTypes);
            var player1NewRating = ratingResults[player1.PlayerId];
            var player2NewRating = ratingResults[player2.PlayerId];
            var player3NewRating = ratingResults[player3.PlayerId];
            Assert.True(player1NewRating < startRating);
            Assert.True(player2NewRating == startRating);
            Assert.True(player1NewRating < player2NewRating, "Loser got greater rating than winner");
            Assert.True(player2NewRating < player3NewRating, "Loser got greater rating than winner");
            Assert.True(player1NewRating > startRating - maxRatingChange);
            Assert.True(player2NewRating < startRating + maxRatingChange);
            Assert.True(player3NewRating < startRating + maxRatingChange);
        }

        private Player GeneratePlayer() => new Player() {  PlayerId = Guid.NewGuid(), Rating = 1000 };

        private PlayerTournamentSession GeneratePlayerTournamentSession(Player player, int tournamentId, int tournamentTypeId, int? score, DateTime joinTime)
        {
            return new PlayerTournamentSession()
            {
                Player = player,
                PlayerId = player.PlayerId,
                TournamentSessionId = tournamentId,
                TournamentTypeId = tournamentTypeId,
                PlayerScore = score,
                JoinTime = joinTime,
            };
        }


        [Fact]
        public void CalculateLeaderboardForPlayer1()
        {
            var player1 = GeneratePlayer();
            var player2 = GeneratePlayer();
            var player3 = GeneratePlayer();
            var player4 = GeneratePlayer();
            var player1TournamentType = new TournamentType()
            {
                TournamentTypeId = 2,
                NumberOfPlayers = 4,
            };

            var player1Session = GeneratePlayerTournamentSession(player1, 1, player1TournamentType.TournamentTypeId, 1000, DateTime.UtcNow - TimeSpan.FromSeconds(5));
            var player2Session = GeneratePlayerTournamentSession(player2, 1, player1TournamentType.TournamentTypeId, 500, DateTime.UtcNow - TimeSpan.FromSeconds(4));
            var player3Session = GeneratePlayerTournamentSession(player3, 1, player1TournamentType.TournamentTypeId + 1, 1500, DateTime.UtcNow - TimeSpan.FromSeconds(3));
            var player4Session = GeneratePlayerTournamentSession(player4, 1, player1TournamentType.TournamentTypeId + 1, 50, DateTime.UtcNow - TimeSpan.FromSeconds(2));


            var allSessions = new[] { player1Session, player2Session, player3Session, player4Session };
            var leaderboard = PostTournamentService.CalculateLeaderboardForPlayer(player1.PlayerId, allSessions, player1TournamentType, 1);
            Assert.NotNull(leaderboard);
            Assert.Equal(4, leaderboard.Count);
            Assert.Equal(leaderboard[0].PlayerId, player3.PlayerId);
            Assert.Equal(leaderboard[0].PlayerScore, 1500);
            Assert.Equal(leaderboard[1].PlayerId, player1.PlayerId);
            Assert.Equal(leaderboard[1].PlayerScore, 1000);
            Assert.Equal(leaderboard[2].PlayerId, player2.PlayerId);
            Assert.Equal(leaderboard[2].PlayerScore, 500);
            Assert.Equal(leaderboard[3].PlayerId, player4.PlayerId);
            Assert.Equal(leaderboard[3].PlayerScore, 50);
        }

        [Fact]
        public void CalculateLeaderboardForPlayersDifferentTournamentType()
        {
            var player1 = GeneratePlayer();
            var player2 = GeneratePlayer();
            var player3 = GeneratePlayer();
            var player4 = GeneratePlayer();
            var player1and2TournamentType = new TournamentType()
            {
                TournamentTypeId = 2,
                NumberOfPlayers = 4,
            };
            var player3and4TournamentType = new TournamentType()
            {
                TournamentTypeId = 1,
                NumberOfPlayers = 2,
            };

            var player3Session = GeneratePlayerTournamentSession(player3, 1, player3and4TournamentType.TournamentTypeId, 1500, DateTime.UtcNow - TimeSpan.FromSeconds(5));
            var player1Session = GeneratePlayerTournamentSession(player1, 1, player1and2TournamentType.TournamentTypeId, 1000, DateTime.UtcNow - TimeSpan.FromSeconds(4));
            var player2Session = GeneratePlayerTournamentSession(player2, 1, player1and2TournamentType.TournamentTypeId, 500, DateTime.UtcNow - TimeSpan.FromSeconds(3));
            var player4Session = GeneratePlayerTournamentSession(player4, 1, player3and4TournamentType.TournamentTypeId, 50, DateTime.UtcNow - TimeSpan.FromSeconds(2));


            var allSessions = new[] { player1Session, player2Session, player3Session, player4Session };
            var tournamentService = new PostTournamentService();
            var leaderboardType2 = PostTournamentService.CalculateLeaderboardForPlayer(player1.PlayerId, allSessions, player1and2TournamentType, 1);
            Assert.NotNull(leaderboardType2);
            Assert.Equal(4, leaderboardType2.Count);
            Assert.Equal(leaderboardType2[0].PlayerId, player3.PlayerId);
            Assert.Equal(leaderboardType2[0].PlayerScore, 1500);
            Assert.Equal(leaderboardType2[1].PlayerId, player1.PlayerId);
            Assert.Equal(leaderboardType2[1].PlayerScore, 1000);
            Assert.Equal(leaderboardType2[2].PlayerId, player2.PlayerId);
            Assert.Equal(leaderboardType2[2].PlayerScore, 500);
            Assert.Equal(leaderboardType2[3].PlayerId, player4.PlayerId);
            Assert.Equal(leaderboardType2[3].PlayerScore, 50);

            var leaderboardType1 = PostTournamentService.CalculateLeaderboardForPlayer(player3.PlayerId, allSessions, player3and4TournamentType, 1);
            Assert.NotNull(leaderboardType1);
            Assert.Equal(2, leaderboardType1.Count);
            Assert.Equal(leaderboardType1[0].PlayerId, player3.PlayerId);
            Assert.Equal(leaderboardType1[0].PlayerScore, 1500);
            Assert.Equal(leaderboardType1[1].PlayerId, player1.PlayerId);
            Assert.Equal(leaderboardType1[1].PlayerScore, 1000);
        }



    }
}