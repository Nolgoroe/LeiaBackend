using System.Data;
using DAL;
using DataObjects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services;
using Services.NuveiPayment;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CustomMatching.Controllers
{


    [Route("[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        public class LoginResponse : Player
        {
            public int? ActiveTournamentSeed { get; set; }
            public int? ActiveTournamentId { get; set; }
            public TournamentType? ActiveTournamentType { get; set; }
            public DateTime? ActiveTournamentEntryTime { get; set; }

            public string AuthToken { get; set; }

            public LoginResponse(Player player)
            {
                this.UpdatePropertiesFrom(player);
            }

        }

        public class RegisterResponse : LoginResponse
        {
            public RegisterResponse(Player player, string secret) : base(player)
            {
                Secret = secret;
            }

            public string Secret { get; set; }
        }

        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;
        private readonly INuveiPaymentService _nuveiPaymentService;

        public PlayersController(ILogger<MatchingController> logger, INuveiPaymentService nuveiPaymentService, ITournamentService tournamentService, ISuikaDbService suikaDbService, IPostTournamentService postTournamentService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            _postTournamentService = postTournamentService;
            _nuveiPaymentService = nuveiPaymentService;
        }


        [HttpPost, Route("GetPlayerTournamentHistory/")]
        public async Task<IActionResult> GetPlayerTournamentHistory([FromBody] BaseAccountRequest request)
        {
            using (var context = new LeiaContext())
            {
                var suikaDbService = new SuikaDbService(context);
                var player = await suikaDbService.LoadPlayerByAuthToken(request.authToken);
                if (player == null) return NotFound("Invalid session auth token");
                var playerId = player.PlayerId;
                try
                {
                    var tournaments = await suikaDbService.GetPlayerTournaments(context, playerId);
                    return Ok(tournaments);
                }
                catch (Exception ex)
                {
                    await suikaDbService.Log(ex, playerId);
                    return StatusCode(500, ex.Message + "\n" + ex.InnerException?.Message);
                }
            }
        }

        // GET /GetPlayerById/5
        [HttpGet, Route("GetPlayerById/{playerId}")]
        public async Task<IActionResult> GetPlayerById(Guid playerId)
        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            return Ok(player);
        }

        [HttpPost, Route("MakePayment/{playerId}")]
        public async Task<IActionResult> MakePayment([FromBody] BaseAccountRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (player == null) return NotFound();
            _logger.LogInformation("Received MakePayment request for playerId \"{PlayerId}\"", player.PlayerId);
            var resp = await _nuveiPaymentService.ProcessPaymentWithCardDetailsAsync(200, "USD", false);
            _logger.LogInformation($"Nuvei payment response {resp.ToString()}");
            dynamic response = new System.Dynamic.ExpandoObject();
            response.Data = resp;
            return Ok(response);
        }

        /// <summary>
        /// Logs in or creates a player. Receives an accountSecret. If its null or empty, then a new player is created and the secret is returned
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost, Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var playerId = _suikaDbService.LeiaContext.PlayerAuthToken.First(s => s.Secret == request.accountSecret).PlayerId;
            var player = await _suikaDbService.LeiaContext.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player != null)
            {
                // Create new auth token
                var newAuthTokenValue = Guid.NewGuid().ToString();
                var authTokenItem = _suikaDbService.LeiaContext.PlayerAuthToken.Find(playerId);
                if (authTokenItem != null)
                {
                    authTokenItem.Token = newAuthTokenValue;
                }
                else
                {
                    authTokenItem = new PlayerAuthToken { PlayerId = playerId, Token = newAuthTokenValue };
                    _suikaDbService.LeiaContext.PlayerAuthToken.Update(authTokenItem);
                }
                _suikaDbService.LeiaContext.SaveChanges();

                var activeMatchMakeRecord = await _suikaDbService.GetPlayerActiveMatchMakeRecord(player.PlayerId);
                int? activeTournamentSeed = null;
                if (activeMatchMakeRecord != null && !activeMatchMakeRecord.IsStillMatchmaking())
                {
                    var tournament = await _suikaDbService.LeiaContext.Tournaments.FindAsync(activeMatchMakeRecord.TournamentId);
                    activeTournamentSeed = tournament.TournamentSeed;
                }
                else
                {
                    activeMatchMakeRecord = null;
                }

                var activeTournamentType = await _suikaDbService.LeiaContext.TournamentTypes.FindAsync(activeMatchMakeRecord?.TournamentTypeId);

                var loginResponse = new LoginResponse(player)
                {
                    ActiveTournamentEntryTime = activeMatchMakeRecord?.JoinTournamentTime,
                    ActiveTournamentId = activeMatchMakeRecord?.TournamentId,
                    ActiveTournamentSeed = activeTournamentSeed,
                    ActiveTournamentType = activeTournamentType,
                    AuthToken = newAuthTokenValue,
                };
                await _suikaDbService.Log($"Player login {player.Name} id={player.PlayerId}, activeTournament?={activeMatchMakeRecord?.TournamentId}", player.PlayerId);
                return Ok(loginResponse);
            }
            else
            {
                return NotFound($"Player: {playerId}, was not found");
            }

        }


        // GET /Players/GetLeagueById/5
        [HttpGet, Route("GetLeagueById/{id}")]
        public async Task<IActionResult> GetLeagueById(int id)
        {
            var league = await _suikaDbService.GetLeagueById(id);
            if (league != null)
            {
                return Ok(league);
            }
            else
            {
                return NotFound($"League: {id}, was not found");
            }

        }


        [HttpPost, Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var player = new Player()
            {
                PlayerId = Guid.NewGuid(),
                Name = request.name,
                Rating = 1500, // TODO: To const
            };
            if (!VerifyPlayer(player)) return BadRequest("Player details are incomplete");
            // check if tournaments with this id already exists
            
            var newPlayerResult = await _suikaDbService.CreateNewPlayer(player);

            return Ok(new RegisterResponse(newPlayerResult.Item1, newPlayerResult.Item2.Secret)
            {
                AuthToken = newPlayerResult.Item2.Token,
            });
        }


        [HttpPost, Route("SetScore")]
        public async Task<IActionResult> SetScore([FromBody] SetScoreRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (player == null) return NotFound();
            var playerId = player.PlayerId;
            var score = request.score;
            var tournamentId = request.tournamentId;
            await _suikaDbService.Log($"Player {playerId} wants to set score {score} for tournament {tournamentId}", playerId);
            try
            {
                if (!await _suikaDbService.RemovePlayerFromActiveTournament(playerId, tournamentId))
                {
                    var message = $"UpdatePlayerTournamentResult: Player {playerId} was not active in tournament {tournamentId}";
                    await _suikaDbService.Log(message, playerId);
                    return BadRequest("Could not submit result, player not in active tournament");
                }
                var tournament = _suikaDbService?.LeiaContext?.Tournaments.FirstOrDefault(t => t.TournamentSessionId == tournamentId && t.IsOpen == true);
                if (tournament == null)
                {
                    await _suikaDbService.Log($"Player {playerId} cannot set score, tournament={tournamentId} not found!", playerId);
                    return BadRequest("Could not submit result, tournament is closed");
                }

                var playerTournament = _suikaDbService?.LeiaContext?.PlayerTournamentSession.Include(pt => pt.TournamentType).FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSession.TournamentSessionId == tournamentId);

                if (playerTournament != null)
                {
                    playerTournament.PlayerScore = score;

                    _suikaDbService.LeiaContext.Entry(playerTournament).State = EntityState.Modified;
                    var updatedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(playerTournament);

                    var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();

                    if (saved > 0)
                    {
                        await _tournamentService.CheckTournamentStatus(_suikaDbService, updatedPlayerTournament.Entity.TournamentSession.TournamentSessionId, playerTournament);
                    }
                    return Ok(updatedPlayerTournament.Entity);
                }
            }
            catch (Exception ex)
            {
                await _suikaDbService.Log(ex, playerId);
            }

            return NotFound($"PlayerTournamentSession was not found for playerId: {playerId}, and tournamentId: {tournamentId}");
        }



        // PUT /Players/ClaimTournamentPrize/1/3FA85F64-5717-4562-B3FC-1A762F63BFC8
        [HttpPost, Route("ClaimTournamentPrize")]
        public async Task<IActionResult> ClaimTournamentPrize([FromBody] ClaimTournamentPrizeRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            
            if (player == null) return NotFound("PlayerId was not provided");

            player = _suikaDbService?.LeiaContext?.Players?.Where(p => p.PlayerId == player.PlayerId)
                .Include(p => p.PlayerCurrencies)
                .FirstOrDefault();
            var tournamentId = request.tournamentId;
            var playerTournamentSession = _suikaDbService.LeiaContext.PlayerTournamentSession.FirstOrDefault(p => p.PlayerId == player.PlayerId && p.TournamentSession.TournamentSessionId == tournamentId);

            if (playerTournamentSession == null)
            {
                return NotFound($"Player {player.PlayerId} was not in tournament {tournamentId}");
            }

            var tournament = _suikaDbService?.LeiaContext?.Tournaments?.Where(t => t.TournamentSessionId == tournamentId)               
                //.Include(td => playerTournamentSession)
                .Include(t => t.PlayerTournamentSessions)
                    .ThenInclude(pt => pt.TournamentType)
                .Include(t => t.Players)
                .FirstOrDefault();

            if (player == null || tournament == null) return NotFound("Player or tournament were not found");
            if (tournament.PlayerTournamentSessions.FirstOrDefault(pt => pt.PlayerId == player.PlayerId && pt.TournamentSession.TournamentSessionId == tournamentId)?.DidClaim != null) return BadRequest("Player already claimed this prize");

           var (amountClaimed, wasTournamentClaimed, PTclaimed ) =  await _postTournamentService.GrantTournamentPrizes(tournament, player);

            if (amountClaimed == null || amountClaimed == -1) return StatusCode(500, $"Returned {amountClaimed}, Failed to claim prize");
            if (wasTournamentClaimed == null || wasTournamentClaimed == false) return StatusCode(500, $"Returned {wasTournamentClaimed}, Failed to claim tournament");

            var leaderboardData = await _suikaDbService.LoadPlayerTournamentLeaderboard(_suikaDbService.LeiaContext, playerId, tournamentId);
            var leaderboard = leaderboardData.Item1;
            var maxPlayers = leaderboardData.Item2;
            var allScoresSubmitted = leaderboard.Count >= maxPlayers && leaderboard.All(s => s.PlayerScore != null);

            if (!allScoresSubmitted) return BadRequest("Tournmanet is not closed");
            return Ok($"Prize claimed successfully: {amountClaimed}. Tournament claimed: {wasTournamentClaimed}. Purple Tokens claimed: {PTclaimed}");
        }

        [HttpPost, Route("GetAllPlayerBalances")]
        public async Task<IActionResult> GetPlayerBalances([FromBody] string authToken)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(authToken);
            if (player == null) return NotFound("PlayerId was not provided");
           
            var balances = await _suikaDbService.GetAllPlayerBalances(player.PlayerId); 
            if (balances != null) return Ok(balances);
            
            else return NotFound("Player balances were not found");
        }        

        // Lital's TODO: This is super insecure. This should only be done from the backend side.
        [HttpPost, Route("UpdatePlayerBalances")]
        public async Task<IActionResult> UpdatePlayerBalances([FromBody] UpdateBalancesRequest updatedValues)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(updatedValues.authToken);
            if (player == null) return NotFound("PlayerId was not provided");
            if (updatedValues == null) return BadRequest("Some values were not provided");
           
            var balances = await _suikaDbService.UpdatePlayerBalance(player.PlayerId, updatedValues.currencyId, updatedValues.amount);
            if (balances != null) return Ok(balances);

            else return NotFound("Player balances were not found");
        }


        private bool VerifyPlayer(Player player)
        {

            if (player != null)
            {
                if (string.IsNullOrWhiteSpace(player.Name) || string.IsNullOrEmpty(player.Name)) return false;
                //enter other verifications here - else if()........
                else return true;
            }
            else return false;
        }
    }
}
