//using CustomMatching.Models;
using Services;
using DataObjects;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DAL;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using static Services.TournamentService;
using NuGet.Protocol;
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

            _tournamentService.MatchTimer.Start();
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

        [HttpGet, Route("RequestMatch/{playerId}/{matchFee}/{currency}")]
        public async Task<IActionResult> RequestMatch(Guid playerId, double matchFee, int currency)
        // [HttpGet, Route("RequestMatch/{playerId}/{tournamentTypeId}")]
        //public async Task<IActionResult> RequestMatch(Guid playerId, int tournamentTypeId)

        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            if (player == null) return NotFound("There is no such id");
            var tournamentType = await _suikaDbService.LeiaContext.Currencies/*TournamentTypes*/.FindAsync(/*tournamentTypeId*/ currency);
            if (tournamentType == null) return NotFound("There is no such Tournament Type");

            var playerBalance = await _suikaDbService.GetPlayerBalance(playerId, currency);
            if (playerBalance == null) return BadRequest("The player doesn't have a balance for this currency");
            if (playerBalance < /*tournamentType?.EntryFee*/ matchFee) return BadRequest("The player doesn't have enough of this currency to join this match");
            else
            {
                MatchRequest request = new()
                {
                    RequestId = new Random().Next(1, 100),
                    Player = player,
                    MatchFee = matchFee/*Convert.ToDouble( tournamentType?.EntryFee)*/,
                    MatchFeeCurrency = tournamentType/*?.Currencies*/
                };
                _tournamentService.MatchesQueue.Add(request);


                // UNSUBSCRIBE FROM THE EVENT! without it, the Controller will be kept alive after it is done and closed. because the PlayerAddedToTournamentHandler is still connected to the event, and that keeps the Controller instance alive, and not garbage collected
                _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
                return Ok($"Match request #{request.RequestId}, added to queue");
            }

        }

        [HttpGet, Route("GetWaitingRequests")]
        public IActionResult GetWaitingRequests()
        {
            var waiting = _tournamentService.WaitingRequests;
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            return Ok(waiting);
        }

        [HttpGet, Route("GetOpenGames")]
        public IActionResult GetOpenGames()
        {
            var openGames = _tournamentService.OngoingTournaments;
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            return Ok(openGames);
        }

        [HttpGet, Route("GetPlayerSeeds")]
        public IActionResult GetPlayerSeeds()
        {
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;

            // we use Newtonsoft.Json here because the default json converter cannot handle nullables    
            var settings = new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore};
            var json = JsonConvert.SerializeObject(_tournamentService?.PlayersSeeds, Formatting.Indented, settings);

            return Ok(json);
        }

        [HttpGet, Route("GetTournamentSeed/{playerId}")]
        public IActionResult GetTournamentSeed(Guid playerId)
        {
            _tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            if (_tournamentService.PlayersSeeds.TryGetValue(playerId, out int?[]? seedAndId))
            {
                _tournamentService.PlayersSeeds.Remove(playerId);
                return Ok(seedAndId);
            }
            else
            {
                return NotFound("The seed or id were not found");
            }
        }

        [HttpGet, Route("GetTournamentTypes")]
        public IActionResult GetTournamentTypes()
        {
            var tournamentTypes = _suikaDbService.LeiaContext.TournamentTypes.Include(tp => tp.Reward).ToList();

            /// DON'T UNSUBSCRIBE FROM THE EVENT HERE! this keeps the PlayerAddedToTournamentHandler  connected to the event, and that makes sure  the PlayerAddedToTournamentHandler method is fired 
            //_tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            return Ok(tournamentTypes);
        }

        [HttpGet, Route("StopTimer")]
        public IActionResult StopTimer()
        {
            _tournamentService.StopTimer();
            return Ok("Timer Stopped");
        }

        [HttpGet, Route("StartTimer")]
        public IActionResult StartTimer()
        {
            _tournamentService.StartTimer();
            return Ok("Timer Started");
        }

        [HttpGet, Route("GetTest")]
        public IActionResult GetTest()
        {

            return Ok("Hello Unity");
        }

        // dump endpoint for testing stuff. DO NOT USE! 
        [HttpGet, Route("TestStuff/{tournamentId}/{playerId}")]
        private async Task<IActionResult> TestStuff(int tournamentId, Guid playerId)
        {
            // await _tournamentService.CheckTournamentStatus(tournamentId);

            var player = _suikaDbService?.LeiaContext?.Players?.Where(p => p.PlayerId == playerId)
                .Include(p => p.PlayerCurrencies)
                .FirstOrDefault();

            var tournament = _suikaDbService?.LeiaContext?.Tournaments?.Where(t => t.TournamentSessionId == tournamentId)
                .Include(t => t.TournamentData)
                    .ThenInclude(td => td.TournamentType)
                .Include(t => t.PlayerTournamentSessions)
                .Include(t => t.Players)
                .FirstOrDefault();

            if (player == null || tournament == null) return NotFound("Player or tournament were not found");
            // await _postTournamentService.GrantTournamentPrizes(tournament, player);
            return Ok();

            /*var playerCurrency = await _suikaDbService.LeiaContext.PlayerCurrencies.FirstOrDefaultAsync(pc => pc.PlayerId == playerId && pc.CurrenciesId == currencyId);

                        var currency = await _suikaDbService.LeiaContext.Currencies.FindAsync(currencyId);

                        var player = await _suikaDbService.LeiaContext.Players.FindAsync(playerId);

                        playerCurrency ??= new();

                        var tournamentTypeId = await _tournamentService.GetTournamentTypeByCurrency(currencyId);
                        var tournament = new TournamentSession
                        {
                            TournamentData = new TournamentData
                            {
                                EntryFee = 10,
                                EntryFeeCurrency = currency,
                                EntryFeeCurrencyId = currency.CurrencyId,
                                EarningCurrencyId = currency.CurrencyId,
                                TournamentTypeId = (int)tournamentTypeId
                            }
                        };
                        tournament.Players?.Add(player);
                        try
                        {
                            var savedTournament = _suikaDbService?.LeiaContext?.Tournaments?.Add(tournament);
                            var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
                            // var balance = _suikaDbService.LeiaContext.PlayerCurrencies.FirstOrDefault(p => p.PlayerId == playerId && p.CurrenciesId == currencyId);
                            return Ok(*//*balance*//*);

                        }
                        catch (Exception ex)
                        {
                            return BadRequest(ex.Message + "\n" + ex.InnerException?.Message);
                        }*/
        }

        [HttpGet, Route("ResetLists")]
        public IActionResult ResetLists()
        {
            _tournamentService.MatchesQueue.Clear();
            _tournamentService.WaitingRequests.Clear();
            _tournamentService.OngoingTournaments.Clear();
            _tournamentService.PlayersSeeds.Clear();
            return Ok($"MatchesQueue count: {_tournamentService.MatchesQueue.Count}\nWaitingRequests count: {_tournamentService.WaitingRequests.Count}\nOngoingTournaments count: {_tournamentService.OngoingTournaments.Count}\nPlayersSeeds count: {_tournamentService.PlayersSeeds.Count}");
        }

    }
}
