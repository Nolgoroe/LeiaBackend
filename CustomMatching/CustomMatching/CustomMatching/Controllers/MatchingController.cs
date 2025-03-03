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
            //OngoingTournaments = new List<MatchSession>();

            //_tournamentService.MatchTimer.Start();
            _tournamentService.PlayerAddedToTournament += PlayerAddedToTournamentHandler;
            // UNSUBSCRIBE FROM THE EVENT! In any endpoint we should unsubscribe from this event because it is subscribed in any call to the Controller. The only exception to that is the GetTournamentTypes(), which is called first and keeps 1 event listener alive so the PlayerAddedToTournamentHandler method will prompt every time the event is raised 
        }

        // the destructor is needed to unsubscribe from the event. without it, the Controller will be kept alive after it is done and closed. because the PlayerAddedToTournamentHandler is still connected to the event, and that keeps the Controller instance alive, and not garbage collected
        ~MatchingController()
        {
            //_tournamentService.MatchTimer.Stop();
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
        }


        private void PlayerAddedToTournamentHandler(object? sender, EventArgs e)
        { /// example how to use ValueTuple types 👇🏻
          ///if (sender is ValueTuple<int?, int?, Guid?[]?>)
            if (sender is SeedData)
            {
                ///var (seed, tournamentId, ids) = (ValueTuple<int?, int?, Guid[]>)sender;
                var seedData = (SeedData)sender;

                // Array.ForEach<Guid>(ids,id =>  _tournamentService?.PlayersSeeds?.Add(id, seed));

                ///foreach (var id in ids)
                foreach (var id in seedData?.Ids)
                {

                    if (id != null && _tournamentService?.PlayersSeeds?.ContainsKey(id) == false)
                    {
                        ///_tournamentService?.PlayersSeeds?.Add(id, [tournamentId, seed]);
                        _tournamentService?.PlayersSeeds?.Add(id, [seedData?.TournamentId, seedData?.Seed]);

                    }
                }
                Trace.WriteLine($"Players: {string.Join(", ", seedData?.Ids/*ids.Select(id => id.ToString())*/)}, in tournament No. {seedData.TournamentId/*tournamentId*/}, with seed No. {seedData?.Seed/*seed*/}, were added");
            }
        }


        public record MatchRequest (string authToken, TournamentType tournamentType);

        [HttpPost, Route("RequestMatch/{playerId}"/*/{matchFee}/{currency}"*/)]
        public async Task<IActionResult> RequestMatch([FromBody] MatchRequest request)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (player == null)
            {
                return NotFound();
            }
            var tournamentType = request.tournamentType;
            var playerId = player.PlayerId;
            // UNSUBSCRIBE FROM THE EVENT! without it, the Controller will be kept alive after it is done and closed. because the PlayerAddedToTournamentHandler is still connected to the event, and that keeps the Controller instance alive, and not garbage collected
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
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

        [HttpGet, Route("GetPlayerSeeds")]
        public IActionResult GetPlayerSeeds()
        {
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;

            // we use Newtonsoft.Json here because the default json converter cannot handle nullables    
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(_tournamentService?.PlayersSeeds, Formatting.Indented, settings);

            return Ok(json);
        }

        [HttpPost, Route("GetTournamentSeed/{playerId}")]
        public async Task<IActionResult> GetTournamentSeed([FromBody] string authToken)
        {
            var player = await _suikaDbService.LoadPlayerByAuthToken(authToken);
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


        // make private before deployment
        [HttpGet, Route("GetTest")]
        public IActionResult GetTest()
        {

            return Ok("Hello Unity");
        }


        [HttpGet, Route("ResetLists")]
        public IActionResult ResetLists()
        {
            _tournamentService.PlayersSeeds.Clear();
            return Ok();
        }


        [HttpGet, Route("ResetPlayersSeeds")]
        public IActionResult ResetPlayersSeeds()
        {
            _tournamentService.PlayersSeeds.Clear();
            return Ok($"PlayersSeeds count: {_tournamentService.PlayersSeeds.Count}");
        }

    }
}
