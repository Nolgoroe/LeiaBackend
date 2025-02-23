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
		public required string userTokenId { get; set; }
		public required string amount { get; set; }
		public required string currency { get; set; }
		public required DeviceDetails deviceDetails { get; set; }
		public required UserPaymentOption userPaymentOption { get; set; }
		public DynamicDescriptor? dynamicDescriptor { get; set; }

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
		public required string userTokenId { get; set; }
		public string? clientUniqueId { get; set; }
		public required string transactionId { get; set; }
		public required string externalTransactionId { get; set; }
		public required string userPaymentOptionId { get; set; }
		public int gwErrorCode { get; set; }
		public required string gwErrorReason { get; set; }
		public int gwExtendedErrorCode { get; set; }
	}
}
