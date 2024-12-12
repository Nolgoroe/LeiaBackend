using Xunit;
using Glicko2;
using Services;
using DataObjects;

namespace ServiceTests
{
    public class PostTournamnetServiceTests
    {
        GlickoPlayer player1 = new GlickoPlayer(ratingDeviation: 200);
        GlickoPlayer player2 = new GlickoPlayer(1400, 30);
        GlickoPlayer player3 = new GlickoPlayer(1550, 100);
        GlickoPlayer player4 = new GlickoPlayer(1700, 300);

        [Fact]
        public void TestCalculatePlayerRating2Player()
        {
            var startRating = 1500;
            var maxRatingChange = 100; // amount of maximum rating drift per each player
            var tournamentSession = new TournamentSession();
            var player1 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
            };
            var playerSession1 = new PlayerTournamentSession()
            {
                PlayerId = player1.PlayerId,
                PlayerScore = 100,
            };
            var player2 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
            };
            var playerSession2 = new PlayerTournamentSession()
            {
                PlayerId = player2.PlayerId,
                PlayerScore = 180,
            };
            Assert.NotEqual(player1.PlayerId, player2.PlayerId);
            tournamentSession.PlayerTournamentSessions.Add(playerSession1);
            tournamentSession.PlayerTournamentSessions.Add(playerSession2);
            tournamentSession.Players.Add(player2);
            tournamentSession.Players.Add(player1);
            var ratingResults = PostTournamentService.CalculatePlayersRatingFromTournament(tournamentSession);
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
            var startRating = 1500;
            var maxRatingChange = 100; // amount of maximum rating drift per each player
            var tournamentSession = new TournamentSession();
            var player1 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
            };
            var playerSession1 = new PlayerTournamentSession()
            {
                PlayerId = player1.PlayerId,
                PlayerScore = 100,
            };
            var player2 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
            };
            var playerSession2 = new PlayerTournamentSession()
            {
                PlayerId = player2.PlayerId,
                PlayerScore = 180,
            };
            var player3 = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Rating = startRating,
            };
            var playerSession3 = new PlayerTournamentSession()
            {
                PlayerId = player3.PlayerId,
                PlayerScore = 200,
            };
            Assert.NotEqual(player1.PlayerId, player2.PlayerId);
            tournamentSession.PlayerTournamentSessions.Add(playerSession1);
            tournamentSession.PlayerTournamentSessions.Add(playerSession2);
            tournamentSession.PlayerTournamentSessions.Add(playerSession3);
            tournamentSession.Players.Add(player3);
            tournamentSession.Players.Add(player2);
            tournamentSession.Players.Add(player1);
            var ratingResults = PostTournamentService.CalculatePlayersRatingFromTournament(tournamentSession);
            var player1NewRating = ratingResults[player1.PlayerId];
            var player2NewRating = ratingResults[player2.PlayerId];
            var player3NewRating = ratingResults[player3.PlayerId];
            Assert.True(player1NewRating < startRating);
            Assert.True(player2NewRating > startRating);
            Assert.True(player1NewRating < player2NewRating, "Loser got greater rating than winner");
            Assert.True(player2NewRating < player3NewRating, "Loser got greater rating than winner");
            Assert.True(player1NewRating > startRating - maxRatingChange);
            Assert.True(player2NewRating < startRating + maxRatingChange);
            Assert.True(player3NewRating < startRating + maxRatingChange);
        }
    }
}