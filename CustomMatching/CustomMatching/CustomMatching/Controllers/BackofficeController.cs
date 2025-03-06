using DataObjects;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.NuveiPayment;

namespace CustomMatching.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class Backoffice : ControllerBase
	{
		private readonly ILogger<MatchingController> _logger;
		private readonly ISuikaDbService _suikaDbService;
		private readonly INuveiPaymentService _nuveiPaymentService;

		public Backoffice(ILogger<MatchingController> logger, INuveiPaymentService nuveiPaymentService, ISuikaDbService suikaDbService)
		{
			_logger = logger;
			_suikaDbService = suikaDbService;
			_nuveiPaymentService = nuveiPaymentService;
		}

		private void ValidateWithdrawalRequest(Player? playerData, WithdrawalDetails? withdrawalDetails, Guid withdrawalMutationToken)
		{
			if (playerData is null
				|| withdrawalDetails is null
				|| withdrawalDetails.PlayerId != playerData.PlayerId
				|| withdrawalDetails.MutationToken != withdrawalMutationToken)
			{
				throw new Exception("Request parameters are invalid");
			}

			if (withdrawalDetails.Status != "PendingProcessing")
			{
				throw new Exception($"Invalid withdrawal status, expecting PendingProcessing, received '{withdrawalDetails.Status}'");
			}

			if (playerData.SavedNuveiPaymentToken is null)
			{
				throw new Exception("Player does not have a saved payment token");
			}
		}

		[HttpGet, Route("ApproveWithdrawal/{playerId}/{withdrawalId}/{withdrawalMutationToken}")]
		public async Task<IActionResult> ApproveWithdrawal(Guid playerId, Guid withdrawalId, Guid withdrawalMutationToken)
		{
			var playerData = await _suikaDbService.GetPlayerById(playerId);
			var withdrawalDetails = await _suikaDbService.GetWithdrawalById(withdrawalId);
			ValidateWithdrawalRequest(playerData, withdrawalDetails, withdrawalMutationToken);

			withdrawalDetails.Status = "ProcessingApproval";
			await _suikaDbService.UpdateWithdrawalDetails(withdrawalDetails);

			// (To a function)
			// Fetch all of the user's payments
			// Fetch all of the user's refunds
			// Decide how many refunds are viable
			// Leftover as payout
			int currencyId = 0;
			double amount = 5.55;
			var resp = await _nuveiPaymentService.ProcessPayoutAsync(playerData.PlayerId.ToString(), playerData.SavedNuveiPaymentToken, amount, currencyId);
			_logger.LogInformation($"Nuvei payment response {resp}");

			withdrawalDetails.Status = "Approved";
			withdrawalDetails.ProcessedAt = DateTime.Now;
			await _suikaDbService.UpdateWithdrawalDetails(withdrawalDetails);

			dynamic response = new System.Dynamic.ExpandoObject();
			return Ok(response);
		}

		[HttpGet, Route("DeclineWithdrawal/{playerId}/{withdrawalId}/{withdrawalMutationToken}")]
		public async Task<IActionResult> DeclineWithdrawal(Guid playerId, Guid withdrawalId, Guid withdrawalMutationToken)
		{
			var playerData = await _suikaDbService.GetPlayerById(playerId);
			var withdrawalDetails = await _suikaDbService.GetWithdrawalById(withdrawalId);
			ValidateWithdrawalRequest(playerData, withdrawalDetails, withdrawalMutationToken);

			withdrawalDetails.Status = "ProcessingDecline";
			await _suikaDbService.UpdateWithdrawalDetails(withdrawalDetails);

			var currentBalance = await _suikaDbService.GetPlayerBalance(playerData.PlayerId, withdrawalDetails.CurrencyId);
			if (currentBalance is null)
			{
				throw new Exception("Could not get the player's balance");
			}
			double restoredBalance = (double)(currentBalance + withdrawalDetails.Amount);
			await _suikaDbService.UpdatePlayerBalance(playerData.PlayerId, withdrawalDetails.CurrencyId, restoredBalance);

			withdrawalDetails.Status = "Declined";
			withdrawalDetails.ProcessedAt = DateTime.Now;
			await _suikaDbService.UpdateWithdrawalDetails(withdrawalDetails);

			dynamic response = new System.Dynamic.ExpandoObject();
			return Ok(response);
		}
	}
}
