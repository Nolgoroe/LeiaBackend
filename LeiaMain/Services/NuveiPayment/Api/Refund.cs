using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#refundTransaction
	/// </summary>
	public class RefundRequest : BaseNuveiApiRequest<RefundResponse>
	{
		public required string amount;
		public required string currency;
		public required string relatedTransactionId;

		public override string[] GetChecksumProperties()
		{
			return [amount, currency, relatedTransactionId];
		}
	}

	/// <summary>
	/// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#refundTransaction
	/// </summary>
	public class RefundResponse : BaseNuveiApiResponse
	{
		public int errCode;
		public required string reason;
		public required string merchantId;
		public required string merchantSiteId;
		public required string version;
		public string? clientRequestId;
		public required string transactionId;
		public required string externalTransactionId;
		public int gwErrorCode;
		public string? gwErrorReason;
		public int gwExtendedErrorCode;
		public required string transactionType;
		public required string customData;
		public required string orderId;
		public string? clientUniqueId;
		public string? userTokenId;
	}
}
