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
        //private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;

        public PlayersController(ILogger<MatchingController> logger, /*ITournamentService tournamentService,*/ ISuikaDbService suikaDbService)
        {
            _logger = logger;
            //_tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
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
            return Ok(player);
        }

        // POST /Players/AddPlayer
        [HttpPost, Route("AddPlayer")]
        public async Task<IActionResult> AddPlayer([FromBody] Player player)
        {
            // check if tournaments with this name already exists
            var dbPlayer = _suikaDbService.GetPlayerByName(player?.Name);
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
            var playerTournament = _suikaDbService?.LeiaContext?.PlayerTournamentSession.FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSessionId == tournamentId);

            if (playerTournament != null)
            {
                playerTournament.PlayerScore = score;
                try
                {
                    _suikaDbService.LeiaContext.Entry(playerTournament).State = EntityState.Modified;
                    var updatedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(playerTournament);

                    var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();
                    return Ok(updatedPlayerTournament.Entity);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + "\n" + ex.InnerException?.Message);
                }
            }

            return NotFound($"PlayerTournamentSession was not found for playerId: {playerId}, and tournamentId: {tournamentId}");
        }

        // DELETE api/<PlayersController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
