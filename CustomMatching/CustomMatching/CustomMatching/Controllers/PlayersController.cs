using System;
using System.Data;
using System.Diagnostics;

using DataObjects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CustomMatching.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;

        public PlayersController(ILogger<MatchingController> logger, ITournamentService tournamentService, ISuikaDbService suikaDbService, IPostTournamentService postTournamentService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            _postTournamentService = postTournamentService;
        }


        // GET /GetPlayerTournamentHistory/5
        [HttpGet, Route("GetPlayerTournamentHistory/{playerId}")]
        public async Task<IActionResult> GetPlayerTournamentHistory(Guid playerId)
        {
            var tournaments = await _suikaDbService.GetPlayerTournaments(playerId);
            return Ok(tournaments);
        }

        // GET /GetPlayerById/5
        [HttpGet, Route("GetPlayerById/{playerId}")]
        public async Task<IActionResult> GetPlayerById(Guid playerId)
        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            return Ok(player);
        }

        // GET /Players/GetPlayerByName/5
        [HttpGet, Route("GetPlayerByName/{name}")]
        public async Task<IActionResult> GetPlayerByName(string name)
        {
            var player = await _suikaDbService.GetPlayerByName(name);
            if (player != null) return Ok(player);
            else return NotFound($"Player: {name}, was not found");

        }

        // POST /Players/AddPlayer
        [HttpPost, Route("AddPlayer")]
        public async Task<IActionResult> AddPlayer([FromBody] Player player)
        {
            if (!VerifyPlayer(player)) return BadRequest("Player details are incomplete");
            // check if tournaments with this name already exists
            var dbPlayer = await _suikaDbService.GetPlayerByName(player?.Name);
            if (dbPlayer == null)
            {
                var newPlayer = await _suikaDbService.AddNewPlayer(player);
                return Ok(newPlayer);
            }
            else return BadRequest("A player with this name already exists");
        }

        // PUT /Players/UpdatePlayerTournamentResult/1/2/3
        [HttpPut, Route("UpdatePlayerTournamentResult/{playerId}/{tournamentId}/{score}")]
        public async Task<IActionResult> UpdatePlayerTournamentResult(Guid playerId, int tournamentId, int score)
        {
            var tournament = _suikaDbService?.LeiaContext?.Tournaments.FirstOrDefault(t => t.TournamentSessionId == tournamentId && t.IsOpen == true);
            if (tournament == null) return BadRequest("Could not submit result, tournament is closed");

            var playerTournament = _suikaDbService?.LeiaContext?.PlayerTournamentSession.FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSessionId == tournamentId);

            if (playerTournament != null)
            {
                playerTournament.PlayerScore = score;
                try
                {
                    _suikaDbService.LeiaContext.Entry(playerTournament).State = EntityState.Modified;
                    var updatedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(playerTournament);

                    var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
                    
                    if (saved > 0)
                    {
                        await _tournamentService.CheckTournamentStatus(updatedPlayerTournament.Entity.TournamentSessionId);
                    }
                    return Ok(updatedPlayerTournament.Entity);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                }
            }

            return NotFound($"PlayerTournamentSession was not found for playerId: {playerId}, and tournamentId: {tournamentId}");
        }


        // PUT /Players/ClaimTournamentPrize/1/3FA85F64-5717-4562-B3FC-1A762F63BFC8
        [HttpPut, Route("ClaimTournamentPrize/{tournamentId}/{playerId}")]
        public async Task<IActionResult> ClaimTournamentPrize(int tournamentId, Guid playerId)
        {

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
            if (tournament.PlayerTournamentSessions.FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSessionId == tournamentId)?.DidClaim != null) return BadRequest("Player already claimed this prize");

           var (amountClaimed, wasTournamentClaimed) =  await _postTournamentService.GrantTournamentPrizes(tournament, player);

            if (amountClaimed == null || amountClaimed == -1) return StatusCode(500, $"Returned {amountClaimed}, Failed to claim prize");
            if (wasTournamentClaimed == null || wasTournamentClaimed == false) return StatusCode(500, $"Returned {wasTournamentClaimed}, Failed to claim tournament");

            return Ok($"Prize claimed successfully: {amountClaimed}. Tournament claimed: {wasTournamentClaimed}");
        }

        // POST/Players/GetAllPlayerBalances
        [HttpPost, Route("GetAllPlayerBalances")]
        public async Task<IActionResult> GetPlayerBalances([FromBody] Guid playerId)
        {
            if (playerId == null) return BadRequest("PlayerId was not provided");
           
            var balances = await _suikaDbService.GetAllPlayerBalances(playerId); 
            if (balances != null) return Ok(balances);
            
            else return NotFound("Player balances were not found");
        }

        // we use record instead of class for the deconstruction ability
        public record UpdateBalancesParams(Guid playerId, int currencyId, double amount);
        // PUT /Players/UpdatePlayerBalances
        [HttpPut, Route("UpdatePlayerBalances")]
        public async Task<IActionResult> UpdatePlayerBalances([FromBody] UpdateBalancesParams updatedValues)
        {
            if (updatedValues == null) return BadRequest("Some values were not provided");
           
            var (playerId, currencyId, amount) = updatedValues;

            var balances = await _suikaDbService.UpdatePlayerBalance(playerId, currencyId, amount);
            if (balances != null) return Ok(balances);

            else return NotFound("Player balances were not found");
        }


        // PUT /Players/UpdatePlayerDetails
        [HttpPut, Route("UpdatePlayerDetails")]
        public async Task<IActionResult> UpdatePlayerDetails([FromBody] Player player)
        {
            if(player == null) return BadRequest("Player was null");
            if (!VerifyPlayer(player)) return BadRequest("Player details are incomplete");
            var dbPlayer = await _suikaDbService.GetPlayerById(player.PlayerId);
            if (dbPlayer == null) return NotFound("Player was not found");



            dbPlayer.UpdatePropertiesFrom(player);
            var updatedPlayer = await _suikaDbService.UpdatePlayer(dbPlayer);
            if (updatedPlayer != null) return Ok(updatedPlayer);
            else return BadRequest("Failed to update player");
        }
       
        // DELETE api/<PlayersController>/5
        [HttpDelete("{id}")]
        private void Delete(int id)
        {
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
