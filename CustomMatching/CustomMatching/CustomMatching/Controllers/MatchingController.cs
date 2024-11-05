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

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatchingController : ControllerBase
    {


        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        //private List<MatchSession> OngoingTournaments;
        //public int Counter { get; set; }
        public MatchingController(ILogger<MatchingController> logger, ITournamentService tournamentService, ISuikaDbService suikaDbService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            //OngoingTournaments = new List<MatchSession>();

            _tournamentService.MatchTimer.Start();
            _tournamentService.PlayerAddedToTournament += PlayerAddedToTournamentHandler;
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
                Debug.WriteLine($"Players: {string.Join(", ", seedData?.Ids/*ids.Select(id => id.ToString())*/)}, in tournament No. {seedData.TournamentId/*tournamentId*/}, with seed No. {seedData?.Seed/*seed*/}, were added");
            }
        }

        [HttpGet, Route("RequestMatch/{playerId}/{matchFee}/{currency}")]
        public async Task<IActionResult> RequestMatch(Guid playerId, double matchFee, int currency)

        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            if (player == null) return NotFound("There is no such id");
            var currenyType = await _suikaDbService.LeiaContext.Currencies.FindAsync(currency);
            if (currenyType == null) return NotFound("There is no such currency");

            var playerBalance = await _suikaDbService.GetPlayerBalance(playerId, currency);
            if (playerBalance == null) return BadRequest("The player doesn't have a balance for this currency");
            if (playerBalance < matchFee) return BadRequest("The player doesn't have enough of this currency to join this match");
            else
            {
                MatchRequest request = new()
                {
                    RequestId = new Random().Next(1, 100),
                    Player = player,
                    MatchFee = matchFee,
                    MatchFeeCurrency = currenyType
                };
                _tournamentService.MatchesQueue.Add(request);

                //_tournamentService.MatchLoopToggle = true;

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

        [HttpGet, Route("GetTournamentSeed/{playerId}")]
        public IActionResult GetTournamentSeed(Guid playerId)
        {
                //_tournamentService.PlayerAddedToTournament -= PlayerAddedToTournamentHandler;
            //!  check why a seedIds is added several times 
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
            var tournamentTypes = _suikaDbService.LeiaContext.TournamentTypes.ToList();
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
        [HttpGet, Route("GetPlayerBalance/{playerId}/{currencyId}")]
        public async Task<IActionResult> GetPlayerBalance(Guid? playerId, int? currencyId)
        {
            var playerCurrency = await _suikaDbService.LeiaContext.PlayerCurrencies.FirstOrDefaultAsync(pc => pc.PlayerId == playerId && pc.CurrenciesId == currencyId);

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
                return Ok(/*balance*/);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message + "\n" + ex.InnerException?.Message);
            }
        }

    }
}
