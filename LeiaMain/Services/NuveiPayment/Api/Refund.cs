using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#refundTransaction
	/// </summary>
	public class RefundRequest : BaseNuveiApiRequest<RefundResponse>
	{
		public required string amount { get; set; }
		public required string currency { get; set; }
		public required string relatedTransactionId { get; set; }

		public override string[] GetChecksumProperties()
		{
			return [amount, currency, relatedTransactionId];
		}
	}

	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#refundTransaction
	/// </summary>
	public class RefundResponse : BaseNuveiApiResponse
	{
		public string? clientRequestId { get; set; }
		public required string transactionId { get; set; }
		public required string externalTransactionId { get; set; }
		public int gwErrorCode { get; set; }
		public string? gwErrorReason { get; set; }
		public int gwExtendedErrorCode { get; set; }
		public required string transactionType { get; set; }
		public required string customData { get; set; }
		public required string orderId { get; set; }
		public string? clientUniqueId { get; set; }
		public string? userTokenId { get; set; }
	}
}
