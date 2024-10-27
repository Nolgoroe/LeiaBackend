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

        private void PlayerAddedToTournamentHandler(object? sender, EventArgs e)
        {
            if (sender is ValueTuple<int?, int?, Guid[]>)
            {
                var (seed, tournamentId, ids) = (ValueTuple<int?, int?, Guid[]>)sender;

                // Array.ForEach<Guid>(ids,id =>  _tournamentService?.PlayersSeeds?.Add(id, seed));
                foreach (var id in ids)
                {
                    if (_tournamentService?.PlayersSeeds?.ContainsKey(id) == false)
                    {
                        _tournamentService?.PlayersSeeds?.Add(id, [tournamentId, seed]);

                    }
                }
                Debug.WriteLine($"Players: {string.Join(", ", ids/*.Select(id => id.ToString())*/)}, in tournament No. {tournamentId}, with seed No. {seed}, were added");
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
            if (playerBalance == null) return BadRequest("The id doesn't have a balance of this currency");
            if (playerBalance < matchFee) return BadRequest("The id doesn't have enough money to join this match");
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
                return Ok($"Match request #{request.RequestId}, added to queue");
            }

        }

        [HttpGet, Route("GetWaitingRequests")]
        public IActionResult GetWaitingRequests()
        {
            var waiting = _tournamentService.WaitingRequests;
            return Ok(waiting);
        }

        [HttpGet, Route("GetOpenGames")]
        public IActionResult GetOpenGames()
        {
            var openGames = _tournamentService.OngoingTournaments;
            return Ok(openGames);
        }

        [HttpGet, Route("GetTournamentSeed/{playerId}")]
        public IActionResult GetTournamentSeed(Guid playerId)
        {
            //! and check why a seedIds is added several times 
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
            return Ok(tournamentTypes);
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
