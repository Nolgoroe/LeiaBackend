using System.Data;
using DAL;
using DataObjects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services;
using Services.NuveiPayment;
using Services.PhoneNumberVerification;

using System.Globalization;
using System.Text.RegularExpressions;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

public class RegistrationAsPayerData
{
    public required string authToken { get; set; }
    public required string phoneNumber { get; set; }
    public required string firstName { get; set; }
    public required string lastName { get; set; }
    public required string country { get; set; }
    public required string email { get; set; }
    public required string birthday { get; set; }
    public required string zipCode { get; set; }
}

public class LoginAsPayerRequest
{
    public required string authToken { get; set; }

    public required string phoneNumber { get; set; }
}

public class ConfirmPhoneNumberRequest
{
    public required string authToken { get; set; }
    public required string phoneNumber { get; set; }
    public required string code { get; set; }
    public RegistrationAsPayerData? registrationAsPayerData { get; set; }
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
        private readonly IPhoneNumberVerificationService _phoneNumberVerificationService;

        public PlayersController(ILogger<MatchingController> logger, INuveiPaymentService nuveiPaymentService, ITournamentService tournamentService, ISuikaDbService suikaDbService, IPostTournamentService postTournamentService, IPhoneNumberVerificationService phoneNumberVerificationService)
        {
            _logger = logger;
            _tournamentService = tournamentService;
            _suikaDbService = suikaDbService;
            _postTournamentService = postTournamentService;
            _nuveiPaymentService = nuveiPaymentService;
            _phoneNumberVerificationService = phoneNumberVerificationService;
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

        private string GetNewAuthToken(Guid playerId)
        {
            string newAuthTokenValue = Guid.NewGuid().ToString();
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

            return newAuthTokenValue;
        }

        private async Task<LoginResponse> GetLoginResponse(Player player, string AuthToken)
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
                ActiveTournamentType = activeTournamentType,
                AuthToken = AuthToken,
            };
            await _suikaDbService.Log($"Player login {player.Name} id={player.PlayerId}, activeTournament?={activeMatchMakeRecord?.TournamentId}", player.PlayerId);
            return loginResponse;
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
            if (player is null)
            {

                return NotFound($"Player: {playerId}, was not found");
            }

            string newAuthTokenValue = GetNewAuthToken(playerId);
            LoginResponse loginResponse = await GetLoginResponse(player, newAuthTokenValue);
            return Ok(loginResponse);
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

        [HttpPost, Route("RegisterAsPayer")]
        public async Task<IActionResult> RegisterAsPayer([FromBody] RegistrationAsPayerData registrationData)
        {
            var playerData = await _suikaDbService.LoadPlayerByAuthToken(registrationData.authToken);
            if (playerData == null)
            {
                return NotFound("PlayerId was not provided");
            }

            if (!VerifyPayerRegistrationData(registrationData))
            {
                return BadRequest("Registration details aren't valid");
            }

            var playerByPhoneNumber = await _suikaDbService.GetPlayerByPhoneNumber(registrationData.phoneNumber);
            if (playerByPhoneNumber is not null && playerByPhoneNumber.IsRegistered)
            {
                return BadRequest("Phone number is already associated to a user");
            }

            await _phoneNumberVerificationService.SendVerificationCode(registrationData.phoneNumber);
            return Ok();
        }

        [HttpPost, Route("LoginAsPayingUser")]
        public async Task<IActionResult> LoginAsPayingUser([FromBody] LoginAsPayerRequest request)
        {
            var playerData = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (playerData == null)
            {
                return NotFound("PlayerId was not provided");
            }

            string phoneNumber = request.phoneNumber;
            if (playerData.PhoneNumber is not null && playerData.PhoneNumber == phoneNumber)
            {
                return BadRequest("Already logged-in");
            }

            var playerDataByPhoneNumber = _suikaDbService.GetPlayerByPhoneNumber(phoneNumber);
            if (playerDataByPhoneNumber is null)
            {
                return NotFound("Player was not found");
            }

            try
            {

                await _phoneNumberVerificationService.SendVerificationCode(phoneNumber);
            }
            catch
            {
                return BadRequest("Could not send a verification code. Please check the phone number and try again.");
            }
            return Ok();
        }

        [HttpPost, Route("ConfirmPhoneNumber")]
        public async Task<IActionResult> ConfirmPhoneNumber([FromBody] ConfirmPhoneNumberRequest request)
        {
            var playerData = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (playerData == null)
            {
                return NotFound("PlayerId was not provided");
            }

            if (playerData.PhoneNumber is not null && playerData.PhoneNumber == request.phoneNumber)
            {
                return BadRequest("Already logged-in"); // and registered
            }


            var registrationData = request.registrationAsPayerData;
            var playerByPhoneNumber = await _suikaDbService.GetPlayerByPhoneNumber(request.phoneNumber);
            if (registrationData is null)
            {
                // Get the player by the phone number- verify that there is one
                if (playerByPhoneNumber is null)
                {
                    return NotFound("Player was not found");
                }

                if (!await _phoneNumberVerificationService.VerifyReceivedCode(request.phoneNumber, request.code))
                {
                    return BadRequest("Bad code");
                }

                string newAuthTokenValue = GetNewAuthToken(playerData.PlayerId);
                LoginResponse playerByPhoneNumberLoginResponse = await GetLoginResponse(playerByPhoneNumber, newAuthTokenValue);
                return Ok(playerByPhoneNumberLoginResponse);
            }


            if (!VerifyPayerRegistrationData(registrationData))
            {
                return BadRequest("Registration details aren't valid");
            }

            if (playerByPhoneNumber is not null)
            {
                return BadRequest("Phone number is already associated to a user");
            }

            try
            {

                if (!await _phoneNumberVerificationService.VerifyReceivedCode(request.phoneNumber, request.code))
                {
                    return BadRequest("Bad code");
                }
            }
            catch
            {
                return BadRequest("Could not send a verification code. Please check the phone number and try again.");
            }

            DateOnly birthday;
            DateOnly.TryParseExact(
                registrationData.birthday,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out birthday);
            playerData.FirstName = registrationData.firstName;
            playerData.LastName = registrationData.lastName;
            playerData.RegistrationDate = DateTime.Now;
            playerData.PhoneNumber = registrationData.phoneNumber;
            playerData.Email = registrationData.email;
            playerData.Country = registrationData.country;
            playerData.Birthday = birthday;
            playerData.ZipCode = registrationData.zipCode;
            await _suikaDbService.UpdatePlayer(playerData);

            LoginResponse loginResponse = await GetLoginResponse(playerData, request.authToken);
            return Ok(loginResponse);
        }

        [HttpPost, Route("SetScore")]
        public async Task<IActionResult> SetScore([FromBody] SetScoreRequest request)
        {
            var callingPlayer = await _suikaDbService.LoadPlayerByAuthToken(request.authToken);
            if (callingPlayer == null) return NotFound();
            var playerId = callingPlayer.PlayerId;
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


                var tournament = _suikaDbService?.LeiaContext?.Tournaments.FirstOrDefault(t => t.TournamentSessionId == tournamentId /*&& t.IsOpen == true*/);
                if (tournament == null)
                {
                    await _suikaDbService.Log($"Player {playerId} cannot set score, tournament={tournamentId} not found!", playerId);
                    return BadRequest("Could not submit result, tournament is closed");
                }

                var playerTournament = _suikaDbService?.LeiaContext?.PlayerTournamentSession.Include(pt => pt.TournamentType).FirstOrDefault(pt => pt.PlayerId == playerId && pt.TournamentSession.TournamentSessionId == tournamentId);

                if (playerTournament != null)
                {
                    playerTournament.PlayerScore = score;
                    playerTournament.SubmitScoreTime = DateTime.UtcNow;

                    _suikaDbService.LeiaContext.Entry(playerTournament).State = EntityState.Modified;
                    var updatedPlayerTournament = _suikaDbService.LeiaContext.PlayerTournamentSession.Update(playerTournament);

                    var saved = await _suikaDbService.LeiaContext.SaveChangesAsync();

                    if (saved > 0)
                    {

                        var allPlayerTournamentSessions = await _suikaDbService.LeiaContext.PlayerTournamentSession.Where(e => e.TournamentSessionId == tournamentId).ToListAsync();

                        var sortedDataForFinalTournamentCalc = PostTournamentService.CalculateLeaderboardForPlayer(playerId, allPlayerTournamentSessions, playerTournament.TournamentType, tournamentId).ToList();

                        //await _tournamentService.CheckTournamentStatus(_suikaDbService, updatedPlayerTournament.Entity.TournamentSession.TournamentSessionId, playerTournament);
                        await _tournamentService.CheckTournamentStatus(sortedDataForFinalTournamentCalc, playerTournament.TournamentType, _suikaDbService, updatedPlayerTournament.Entity.TournamentSession.TournamentSessionId, callingPlayer);
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

            var leaderboardData = await _suikaDbService.LoadPlayerTournamentLeaderboard(_suikaDbService.LeiaContext, player.PlayerId, tournamentId);
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

        private bool VerifyPayerRegistrationData(RegistrationAsPayerData? registrationData)
        {
            if (registrationData is null)
            {
                return false;
            }

            // Validate first name and last name (non-empty, non-whitespace, and at least 2 characters long)
            if (string.IsNullOrWhiteSpace(registrationData.firstName) || registrationData.firstName.Length < 2)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(registrationData.lastName) || registrationData.lastName.Length < 2)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(registrationData.lastName) || registrationData.zipCode.Length < 5)
            {
                return false;
            }

            // Validate country: must be exactly 2 characters
            if (string.IsNullOrWhiteSpace(registrationData.country) || registrationData.country.Length != 2)
            {
                return false;
            }

            // Validate email using a regular expression
            var emailRegex = new Regex("^[A-Z0-9._+-]+@[A-Z0-9._-]+\\.[A-Z]{2,}$", RegexOptions.IgnoreCase);
            if (string.IsNullOrWhiteSpace(registrationData.email) || !emailRegex.IsMatch(registrationData.email))
            {
                return false;
            }

            // Validate birthday format using a regex for ISO format (yyyy-MM-dd)
            var birthdayRegex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
            if (string.IsNullOrWhiteSpace(registrationData.birthday) || !birthdayRegex.IsMatch(registrationData.birthday))
            {
                return false;
            }
            // Try to parse the birthday.
            if (!DateTime.TryParseExact(
                    registrationData.birthday,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime birthday))
            {
                return false;
            }

            // Verify the user is at least 18 years old (to the day)
            DateTime today = DateTime.Today;
            DateTime minBirthdate = today.AddYears(-18);
            if (birthday > minBirthdate)
            {
                return false;
            }

            // Validate phone number:
            // US: +1 followed by exactly 10 digits.
            // Japan: +81 followed by 9 or 10 digits.
            // Israel: +972 followed by 8 or 9 digits.


            var phoneRegex = new Regex(@"^(\+1\d{10}|\+81\d{9,10}|\+972\d{8,10})$");
            if (string.IsNullOrWhiteSpace(registrationData.phoneNumber) || !phoneRegex.IsMatch(registrationData.phoneNumber))
            {
                return false;
            }

            return true;
        }
    }
}
