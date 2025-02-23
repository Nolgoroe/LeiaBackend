using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payout
	/// </summary>
	public class PayoutRequest : BaseNuveiApiRequest<PayoutResponse>
	{
		public required string userTokenId;
		public required string amount;
		public required string currency;
		public required DeviceDetails deviceDetails;
		public required UserPaymentOption userPaymentOption;
		public DynamicDescriptor? dynamicDescriptor;

		public override string[] GetChecksumProperties()
		{
			return [amount, currency];
		}
	}

	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payout
	/// </summary>
	public class PayoutResponse : BaseNuveiApiResponse
	{
		public required string merchantId;
		public required string merchantSiteId;
		public required string userTokenId;
		public string? clientUniqueId;
		public required string transactionId;
		public required string externalTransactionId;
		public required string userPaymentOptionId;
		public int errCode;
		public required string reason;
		public int gwErrorCode;
		public required string gwErrorReason;
		public int gwExtendedErrorCode;
		public required string version;
	}
}
