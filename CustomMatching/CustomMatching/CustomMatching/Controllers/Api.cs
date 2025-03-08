using DataObjects;

namespace CustomMatching.Controllers
{
    
    public record LoginRequest (string accountSecret);

    public record RegisterRequest(string name);

    public record BaseAccountRequest
    {
        public required string authToken { get; set; }
    }
    
    public record ClaimTournamentPrizeRequest(int tournamentId) :  BaseAccountRequest;

    public record MatchRequest(int tournamentTypeId, int gameTypeId) : BaseAccountRequest;

    public record UpdateBalancesRequest(int currencyId, double amount) : BaseAccountRequest;

    public record SetScoreRequest(int tournamentId, int score) : BaseAccountRequest;

}