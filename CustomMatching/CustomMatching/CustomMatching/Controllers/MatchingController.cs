//using CustomMatching.Models;
using Services;
using DataObjects;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatchingController : ControllerBase
    {


        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        private List<Player> _players;
        //private List<MatchSession> OngoingTournaments;
        //public bool MatchLoopToggle { get; private set; }
        //public int Counter { get; set; }
        public MatchingController(ILogger<MatchingController> logger, ITournamentService tournamentService, ISuikaDbService suikaDbService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            //OngoingTournaments = new List<MatchSession>();
            _players = new List<Player>()
            {
                //new Player{PlayerId ="4", USD = 20,Score = 1000 },
                //new Player{PlayerId ="7", USD = 15,Score = 500 },
                //new Player{PlayerId ="3", USD = 21,Score = 1100 },
                //new Player{PlayerId ="5", USD = 19,Score = 900 },
                //new Player{PlayerId ="9", USD = 10,Score = 400 },
                //new Player{PlayerId ="2", USD = 110,Score = 1400 },
                //new Player{PlayerId ="8", USD = 14,Score = 400 },
                //new Player{PlayerId ="1", USD = 111,Score = 1500 },
                //new Player{PlayerId ="6", USD = 16,Score = 600 },
            };

            //MatchLoopToggle = true;
            _tournamentService.MatchTimer.Start();
        }

        [HttpGet, Route("RequestMatch/{playerId}/{matchFee}/{currency}")]
        public async Task<IActionResult> RequestMatch(Guid playerId, double matchFee, int currency)

        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            if (player == null) return NotFound("There is no such player RequestId");
            var playerBalance = await _suikaDbService.GetPlayerBalance(playerId,currency);
            if (playerBalance == null) return BadRequest("The player doesn't have a balance of this currency");
            if (playerBalance < matchFee) return BadRequest("The player doesn't have enough money to join this match");
            else
            {
                MatchRequest request = new()
                {
                    RequestId = new Random().Next(1, 100),
                    Player = player,
                    MatchFee = matchFee
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

        [HttpGet, Route("GetTest")]
        public IActionResult GetTest()
        {
           
            return Ok("Hello Unity");
        }

        [HttpPost, Route("AddPlayer")]
        public async Task<IActionResult> AddPlayer([FromBody] Player player)
        {
            var newPlayer = await _suikaDbService.AddNewPlayer(player);
            return Ok(newPlayer);
        }

    }
}
