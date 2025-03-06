using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DataObjects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services;
using Services.Emailer;
using Services.NuveiPayment;
using Services.NuveiPayment.Api;
using static CustomMatching.Controllers.PlayersController;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

public class PaymentRequestBody
{
    public string CardNumber { get; set; }
    public string CVV { get; set; }
    public string ExpMonth { get; set; }
    public string ExpYear { get; set; }
}

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

            public LoginResponse(Player player)
            {
                this.UpdatePropertiesFrom(player);
            }

        }

        private readonly ILogger<MatchingController> _logger;
        private readonly ITournamentService _tournamentService;
        private readonly ISuikaDbService _suikaDbService;
        private readonly IPostTournamentService _postTournamentService;
        private readonly INuveiPaymentService _nuveiPaymentService;
        private readonly IEmailService _emailService;

        public PlayersController(ILogger<MatchingController> logger, INuveiPaymentService nuveiPaymentService, ITournamentService tournamentService, ISuikaDbService suikaDbService, IPostTournamentService postTournamentService, IEmailService emailService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            _postTournamentService = postTournamentService;
            _nuveiPaymentService = nuveiPaymentService;
            _emailService = emailService;
        }


        // GET /GetPlayerTournamentHistory/5
        [HttpGet, Route("GetPlayerTournamentHistory/{playerId}")]
        public async Task<IActionResult> GetPlayerTournamentHistory(Guid playerId)
        {
            try
            {
                var tournaments = await _suikaDbService.GetPlayerTournaments(_suikaDbService.LeiaContext, playerId);
                return Ok(tournaments);
            }
            catch (Exception ex)
            {
                await _suikaDbService.Log(ex, playerId);
                return StatusCode(500, ex.Message + "\n" + ex.InnerException?.Message);
            }
        }

        // GET /GetPlayerById/5
        [HttpGet, Route("GetPlayerById/{playerId}")]
        public async Task<IActionResult> GetPlayerById(Guid playerId)
        {
            var player = await _suikaDbService.GetPlayerById(playerId);
            return Ok(player);
        }

        private async Task CompleteDepositActions(Player playerData, int currencyId, double amount, PaymentResponse resp)
        {
            PaymentDetails paymentDetails = new PaymentDetails
            {
                PaymentId = Guid.NewGuid(),
                PlayerId = playerData.PlayerId,
                CreatedAt = DateTime.Now,
                Amount = amount,
                ProcessorTransactionId = resp.transactionId,
                PaymentOptionId = "",
                ResponseBody = JsonSerializer.Serialize(resp, resp.GetType())
            };
            await _suikaDbService.CreatePaymentDetails(paymentDetails);

            await _suikaDbService.UpdatePlayerBalance(playerData.PlayerId, currencyId, amount);
        }

        [HttpPost, Route("MakePayment/{playerId}/{currencyId}/{paymentOptionId}")]
        public async Task<IActionResult> MakePayment(Guid playerId, int currencyId, string paymentOptionId, [FromBody] PaymentRequestBody paymentRequest)
        {
            if (currencyId < 0 || paymentOptionId is null)
            {
                // TODO: validations on the paymentOptionId
                return BadRequest("Invalid currency or payment option.");
            }
            int expMonth;
            Int32.TryParse(paymentRequest.ExpMonth, out expMonth);
            int expYear;
            Int32.TryParse(paymentRequest.ExpYear, out expYear);
            if (paymentRequest.CardNumber.Length != 16 || !(paymentRequest.CVV.Length == 3 || paymentRequest.CVV.Length == 4) || expMonth <= 0 || expMonth > 12 || expYear < 2025 || expYear > 2050)
            {
                return BadRequest("Invalid card information.");
            }

            var playerData = await _suikaDbService.GetPlayerById(playerId);
            if (playerData is null)
            {
                return NotFound("Player was not found");
            }

            // TODO: After registration is available- verify that the user is registered + all of the relevant properties are present

            PaymentOptionCard card = new PaymentOptionCard
            {
                cardNumber = paymentRequest.CardNumber,
                cardHolderName = "John Doe",
                expirationMonth = paymentRequest.ExpMonth,
                expirationYear = paymentRequest.ExpYear,
                CVV = paymentRequest.CVV
            };
            // TODO: payment option
            double amount = 2.00;
            PaymentResponse resp = await _nuveiPaymentService.ProcessPaymentWithCardDetailsAsync(card, amount, currencyId, false);
            _logger.LogInformation($"Nuvei payment response {resp.ToString()}");

            // TODO: pass payment option
            await CompleteDepositActions(playerData, currencyId, amount, resp);

            var nuveiPaymentToken = resp.paymentOption?.userPaymentOptionId;
            await _suikaDbService.UpdatePlayerSavedNuveiPaymentToken(playerData.PlayerId, nuveiPaymentToken);

            dynamic response = new System.Dynamic.ExpandoObject();
            response.Data = new { nuveiPaymentToken = nuveiPaymentToken };
            return Ok(response);
        }

        [HttpPost, Route("MakePaymentWithSavedToken/{playerId}/{currencyId}/{paymentOptionId}")]
        public async Task<IActionResult> MakePaymentWithSavedToken(Guid playerId, int currencyId, string paymentOptionId)
        {
            if (currencyId < 0 || paymentOptionId is null)
            {
                // TODO: validations on the paymentOptionId
                return BadRequest("Invalid currency or payment option.");
            }

            var playerData = await _suikaDbService.GetPlayerById(playerId);
            if (playerData is null)
            {
                return NotFound("Player was not found");
            }
            if (playerData.SavedNuveiPaymentToken is null || playerData.SavedNuveiPaymentToken == "")
            {
                return BadRequest("User does not have a saved payment token");
            }

            double amount = 2.00;
            PaymentResponse resp = await _nuveiPaymentService.ProcessPaymentWithTokenAsync(playerData.PlayerId, playerData.SavedNuveiPaymentToken, amount, currencyId, false);
            _logger.LogInformation($"Nuvei saved-token payment response {resp}");
            await CompleteDepositActions(playerData, currencyId, amount, resp);

            dynamic response = new System.Dynamic.ExpandoObject();
            return Ok(response);
        }

        [HttpPost, Route("CreateWithdrawal/{playerId}/{currencyId}/{amount}")]
        public async Task<IActionResult> MakeWithdraw(Guid playerId, int currencyId, double amount)
        {
            if (currencyId < 0 || amount <= 0)
            {
                return BadRequest("Invalid currency or amount.");
            }

            var playerData = await _suikaDbService.GetPlayerById(playerId);
            if (playerData is null)
            {
                return NotFound("Player was not found");
            }
            var playerBalance = await _suikaDbService.GetPlayerBalance(playerData.PlayerId, currencyId);
            if (playerBalance is null || amount > playerBalance)
            {
                return BadRequest("Not enough money in the balance. Please try a lower amount.");
            }

            var latestWithdrawal = await _suikaDbService.GetLatestWithdrawalDetails(playerData.PlayerId);
            string[] finalWithdrawalStatuses = { "Success", "Denied" };
            if (latestWithdrawal?.Status is not null && !finalWithdrawalStatuses.Contains(latestWithdrawal.Status))
            {
                return BadRequest("Only one withdrawal can be processed at a time.");
            }

            if (!await _suikaDbService.DoesPlayerHaveSuccessfulPayment(playerData.PlayerId))
            {
                return BadRequest("Only players with a successful payment can make a withdrawal.");
            }

            WithdrawalDetails withdrawalDetails = new WithdrawalDetails
            {
                WithdrawalId = Guid.NewGuid(),
                PlayerId = playerData.PlayerId,
                CreatedAt = DateTime.Now,
                MutationToken = Guid.NewGuid(),
                CurrencyId = currencyId,
                Amount = amount,
                Status = "EmailNotSent",
            };
            await _suikaDbService.CreateWithdrawalDetails(withdrawalDetails);

            await _suikaDbService.UpdatePlayerBalance(playerData.PlayerId, currencyId, -1 * amount);

            string emailSubject = $"Withdrawal request from player \"{playerData.PlayerId}\"";
            string approveLink = $"https://leiagames.com/Backoffice/ApproveWithdrawal/{playerId}/{withdrawalDetails.WithdrawalId}/{withdrawalDetails.MutationToken}";
            string declineLink = $"https://leiagames.com/Backoffice/DeclineWithdrawal/{playerId}/{withdrawalDetails.WithdrawalId}/{withdrawalDetails.MutationToken}";
            // TODO: add currency sign / code
            string emailBody = $"Withdrawal request for {amount}<br />Player name: {playerData.Name}<br />Player ID: {playerData.PlayerId}<br /><br /><br /><a href=\"{approveLink}\">Approve the withdrawal</a><br /><br /><br /><a href=\"{declineLink}\">Decline the withdrawal</a>";
            _emailService.SendEmail("support@leia.games", emailSubject, emailBody);

            withdrawalDetails.Status = "PendingProcessing";
            await _suikaDbService.UpdateWithdrawalDetails(withdrawalDetails);

            dynamic response = new System.Dynamic.ExpandoObject();
            return Ok(response);
        }

        // GET /Players/GetPlayerByName/"test01"
        [HttpGet, Route("GetPlayerByName/{name}")]
        public async Task<IActionResult> GetPlayerByName(string name)
        {
            var player = await _suikaDbService.GetPlayerByName(name);
            if (player != null)
            {
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
                    ActiveTournamentType = activeTournamentType
                };
                await _suikaDbService.Log($"Player login {player.Name} id={player.PlayerId}, activeTournament?={activeMatchMakeRecord?.TournamentId}", player.PlayerId);
                return Ok(loginResponse);
            }
            else
            {
                return NotFound($"Player: {name}, was not found");
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




        // POST /Players/AddPlayer
        [HttpPost, Route("AddPlayer")]
        public async Task<IActionResult> AddPlayer([FromBody] Player player)
        {
            if (!VerifyPlayer(player)) return BadRequest("Player details are incomplete");
            // check if tournaments with this id already exists
            var dbPlayer = await _suikaDbService.GetPlayerByName(player?.Name);
            if (dbPlayer == null)
            {
                var newPlayer = await _suikaDbService.AddNewPlayer(player);
                return Ok(newPlayer);
            }
            else return BadRequest("A player with this id already exists");
        }

        // PUT /Players/UpdatePlayerTournamentResult/1/2/3
        [HttpPut, Route("UpdatePlayerTournamentResult/{playerId}/{tournamentId}/{score}")]
        public async Task<IActionResult> UpdatePlayerTournamentResult(Guid playerId, int tournamentId, int score)
        {
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
        [HttpPut, Route("ClaimTournamentPrize/{tournamentId}/{playerId}")]
        public async Task<IActionResult> ClaimTournamentPrize(int tournamentId, Guid playerId)
        {

            var player = _suikaDbService?.LeiaContext?.Players?.Where(p => p.PlayerId == playerId)
                .Include(p => p.PlayerCurrencies)
                .FirstOrDefault();

            var playerTournamentSession = _suikaDbService.LeiaContext.PlayerTournamentSession.FirstOrDefault(p => p.PlayerId == playerId && p.TournamentSession.TournamentSessionId == tournamentId);

            if (playerTournamentSession == null)
            {
                return NotFound($"Player {playerId} was not in tournament {tournamentId}");
            }

            var tournament = _suikaDbService?.LeiaContext?.Tournaments?.Where(t => t.TournamentSessionId == tournamentId)
                //.Include(td => playerTournamentSession)
                .Include(t => t.PlayerTournamentSessions)
                    .ThenInclude(pt => pt.TournamentType)
                .Include(t => t.Players)
                .FirstOrDefault();

            if (player == null || tournament == null) return NotFound("Player or tournament were not found");
            if (tournament.PlayerTournamentSessions.FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSession.TournamentSessionId == tournamentId)?.DidClaim != null) return BadRequest("Player already claimed this prize");

            var (amountClaimed, wasTournamentClaimed, PTclaimed) = await _postTournamentService.GrantTournamentPrizes(tournament, player);

            if (amountClaimed == null || amountClaimed == -1) return StatusCode(500, $"Returned {amountClaimed}, Failed to claim prize");
            if (wasTournamentClaimed == null || wasTournamentClaimed == false) return StatusCode(500, $"Returned {wasTournamentClaimed}, Failed to claim tournament");

            return Ok($"Prize claimed successfully: {amountClaimed}. Tournament claimed: {wasTournamentClaimed}. Purple Tokens claimed: {PTclaimed}");
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
            if (player == null) return BadRequest("Player was null");
            if (!VerifyPlayer(player)) return BadRequest("Player details are incomplete");
            var dbPlayer = await _suikaDbService.GetPlayerById(player.PlayerId);
            if (dbPlayer == null) return NotFound("Player was not found");



            dbPlayer.UpdatePropertiesFrom(player);
            var updatedPlayer = await _suikaDbService.UpdatePlayer(dbPlayer);
            if (updatedPlayer != null) return Ok(updatedPlayer);
            else return BadRequest("Failed to update league");
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
