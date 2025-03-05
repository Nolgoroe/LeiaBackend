using Services;
using DataObjects;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using static Services.TournamentService;
using Newtonsoft.Json;


namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatchingController : ControllerBase
    {


        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;

        //private List<MatchSession> OngoingTournaments;
        //public int Counter { get; set; }
        public MatchingController(ILogger<MatchingController> logger, ITournamentService tournamentService, ISuikaDbService suikaDbService, IPostTournamentService postTournamentService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            _postTournamentService = postTournamentService;
        }



        [HttpPost, Route("RequestMatch")]
        public async Task<IActionResult> RequestMatch([FromBody] MatchRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (player == null)
            {
                return NotFound($"Invalid auth token");
            }
            var tournamentType = _suikaDbService.LeiaContext.TournamentTypes.Find(request.tournamentTypeId);
            if (tournamentType == null)
            {
                return NotFound($"No such tournament type: {request.tournamentTypeId}");
            }
            var playerId = player.PlayerId;

            var isSuccess = false;

            try
            {
                #region Validate request

                var currencies = await _suikaDbService.LeiaContext.Currencies.FindAsync(tournamentType.CurrenciesId/*currency*/);
                if (currencies == null) return NotFound("There is no such currency");

                var dbTournamentType = await _suikaDbService.LeiaContext.TournamentTypes.FindAsync(tournamentType.TournamentTypeId);
                if (dbTournamentType == null) return NotFound("There is no such Tournament Type");

                var playerBalance = await _suikaDbService.GetPlayerBalance(playerId, dbTournamentType?.CurrenciesId/*currency*/);
                if (playerBalance == null) return BadRequest("The player doesn't have a balance for this currency");

                if (playerBalance < dbTournamentType?.EntryFee /* matchFee*/) return BadRequest(new { IsSuccess = false, ErrorMessage = "The player doesn't have enough of this currency to join this match", currencyId = dbTournamentType.CurrenciesId });

                #endregion

                var canMatchMake = await _suikaDbService.MarkPlayerAsMatchMaking(player.PlayerId, tournamentType.EntryFee.Value, currencies.CurrencyId, tournamentType.TournamentTypeId);

                if (!canMatchMake)
                {
                    // If we're in a tournament that timedout - allow to perform a new match
                    var matchmakeRecord = await _suikaDbService.GetPlayerActiveMatchMakeRecord(player.PlayerId);
                    if (matchmakeRecord != null && !matchmakeRecord.IsStillMatchmaking() && (DateTime.UtcNow - matchmakeRecord.MatchmakeStartTime) > TimeSpan.FromMinutes(15))
                    {
                        await _suikaDbService.Log("Removing player from tournament due to timeout so the player can matchmake", playerId);
                        await _suikaDbService.RemovePlayerFromActiveTournament(playerId, matchmakeRecord.TournamentId);
                        return await RequestMatch(request);
                    }
                    return BadRequest("Player cannot match make");
                }
                else
                {
                    isSuccess = true;
                    return Ok($"Match request, added to queue");
                }
            }
            finally
            {
                if (!isSuccess)
                {
                    // If we didn't manage to complete the operation, make sure to clear up the PlayerActiveTournament record, otherwise the player won't be able to match make again
                    await _suikaDbService.RemovePlayerFromActiveMatchMaking(playerId);
                }
            }



        }

        [HttpPost, Route("GetTournamentSeed/")]
        public async Task<IActionResult> GetTournamentSeed([FromBody] BaseAccountRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (player == null) return NotFound("Couldn't find player");
            var playerId = player.PlayerId;
            try
            {
                #region Active Tournaments Part
                var matchmakeRecord = await _suikaDbService.GetPlayerActiveMatchMakeRecord(playerId);
                if (matchmakeRecord == null)
                {
                    return NotFound("Player not in tournament");
                }
                if (matchmakeRecord.IsStillMatchmaking())
                {
                    return NotFound("Still match making");
                }
                #endregion


                var tournament = await _suikaDbService.LeiaContext.Tournaments.FindAsync(matchmakeRecord.TournamentId);
                if (tournament == null)
                {
                    await _suikaDbService.Log($"GetTournamentSeed: Tournament {matchmakeRecord.TournamentId} does not exist!", playerId);
                    return StatusCode(500, "Could not find tournament for player");
                }
                // Already charged? Just return the seed
                if (matchmakeRecord.DidCharge)
                {
                    return Ok(new int[] { tournament.TournamentSessionId, tournament.TournamentSeed });
                }

                if (tournament != null)
                {
                    #region CHARGE PLAYER Part
                    // TODO: Lock inside a semaphore

                    var newBalance = await _tournamentService.ChargePlayer(playerId, tournament.TournamentSessionId); // we use IdAndSeed[0] to get tournament Id because it first int the array
                    if (newBalance != null)
                    {
                        await _suikaDbService.Log($"Player {playerId}, was charged for tournament: {tournament.TournamentSessionId}. New balance is: {newBalance?.CurrencyBalance}, currency type is: {newBalance?.CurrenciesId}", playerId);
                        await _suikaDbService.MarkMatchMakeEntryAsCharged(playerId, tournament.TournamentSessionId);
                    }
                    else
                    {
                        await _suikaDbService.Log($"Failed to charge player {playerId} for tournament: {tournament.TournamentSessionId}. New balance is: {newBalance?.CurrencyBalance}", playerId);
                        return StatusCode(500, "Failed to charge player for the tournament");
                    }
                    #endregion
                }
                else
                {
                    await _suikaDbService.Log($"Could not add player to tournament... unknown reason", playerId);
                    return StatusCode(500, "Could not find tournament for player");
                }
                return Ok(new int[] { tournament.TournamentSessionId, tournament.TournamentSeed });
            }
            catch (Exception ex)
            {
                await _suikaDbService.Log(ex, playerId);
                return StatusCode(500, $"Encountered error: {ex.Message + "\n" + ex.InnerException?.Message}");
            }
        }

        [HttpGet, Route("GetTournamentTypes")]
        public IActionResult GetTournamentTypes()
        {
            var tournamentTypes = _suikaDbService.LeiaContext.TournamentTypes.Include(tp => tp.Reward).OrderBy(tp => tp.CurrenciesId).ToList();

            /// DON'T UNSUBSCRIBE FROM THE EVENT HERE! this keeps the PlayerAddedToTournamentHandler  connected to the event, and that makes sure  the PlayerAddedToTournamentHandler method is fired 
            //_tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            return Ok(tournamentTypes);
        }
    }
}
