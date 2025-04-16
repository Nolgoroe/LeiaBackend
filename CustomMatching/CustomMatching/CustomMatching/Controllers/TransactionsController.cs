using DAL;
using DataObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services;



public class GetAllPlayerTransactionRequest
{
    public string PlayerAuthToken { get; set; }
}

public class AddTransactionRequest
{
    public string PlayerAuthToken { get; set; }
    public int CurrenciesID { get; set; }
    public decimal CurrenciesAmount { get; set; }
    public string TransactionTypeName { get; set; }
}
namespace CustomMatching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TransactionsController : ControllerBase
    {

        private readonly ISuikaDbService _suikaDbService;

        public TransactionsController(LeiaContext context, ISuikaDbService suikaDbService)
        {
            _suikaDbService = suikaDbService;
        }


        [HttpPost("add")]
        public async Task<IActionResult> AddTransaction([FromBody] AddTransactionRequest request)
        {
            // Validate input
            if (string.IsNullOrEmpty(request.PlayerAuthToken))
            {
                return BadRequest("PlayerAuthToken is required.");
            }

            // Retrieve player based on authentication token
            var player = await _suikaDbService.LoadPlayerByAuthToken(request.PlayerAuthToken);
            if (player == null)
            {
                return Unauthorized("Invalid or expired authentication token.");
            }

            var transactionType = await _suikaDbService.LeiaContext.TransactionTypes
                .FirstOrDefaultAsync(t => t.TransactionTypeName == request.TransactionTypeName);

            if (transactionType == null)
            {
                transactionType = new TransactionType
                {
                    TransactionTypeName = request.TransactionTypeName,
                };
                _suikaDbService.LeiaContext.TransactionTypes.Add(transactionType);
                await _suikaDbService.LeiaContext.SaveChangesAsync();
            }

            // Create a new transaction record
            var newTransaction = new Transactions
            {
                PlayerId = player.PlayerId,
                TransactionDate = DateTime.UtcNow,
                CurrenciesId = request.CurrenciesID,
                CurrencyAmount = request.CurrenciesAmount,
                TransactionTypeId = transactionType.TransactionTypeId,
                TransactionTypeName = request.TransactionTypeName
            };

            // Add new transaction to the context
            _suikaDbService.LeiaContext.Transactions.Add(newTransaction);

            try
            {
                // Save the changes to the database
                await _suikaDbService.LeiaContext.SaveChangesAsync();
                await _suikaDbService.Log($"New transaction added: {newTransaction.TransactionId} for player {player.PlayerId}.", player.PlayerId);
                return Ok(new { success = true, transactionId = newTransaction.TransactionId });
            }
            catch (Exception ex)
            {
                await _suikaDbService.Log(ex, player.PlayerId);
                return StatusCode(500, $"An error occurred while adding the transaction: {ex.Message}");
            }
        }


        [HttpPost("get all")]
        public async Task<IActionResult> GetTransactionsByPlayerAuth([FromBody] GetAllPlayerTransactionRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerAuthToken))
            {
                return BadRequest("PlayerAuthToken is required.");
            }

            var player = await _suikaDbService.LoadPlayerByAuthToken(request.PlayerAuthToken);
            if (player == null)
            {
                return Unauthorized("Invalid or expired authentication token.");
            }

            try
            {
                var transactions = await _suikaDbService.LeiaContext.Transactions
                    .Where(t => t.PlayerId == player.PlayerId)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();

                return Ok(transactions);

                //return Ok(new
                //{
                //    success = true,
                //    playerId = player.PlayerId,
                //    transactions = transactions
                //});
            }
            catch (Exception ex)
            {
                await _suikaDbService.Log(ex, player.PlayerId);
                return StatusCode(500, $"An error occurred while retrieving transactions: {ex.Message}");
            }
        }
    }
}

