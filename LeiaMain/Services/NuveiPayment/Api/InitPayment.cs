using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#initPayment
	/// </summary>
	public class InitPaymentRequest : BaseNuveiApiRequest<InitPaymentResponse>
	{
		public string? userTokenId;
		public required string amount;
		public required string currency;
		public required PaymentOptionRoot paymentOption;
		public required DeviceDetails deviceDetails;
		public required BillingAddressDetails billingAddress;

		public override string[] GetChecksumProperties()
		{
			return [amount, currency];
		}
	}

	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#initPayment
	/// </summary>
	public class InitPaymentResponse : BaseNuveiApiResponse
	{
		public int errCode;
		public required string reason;
		public required string merchantId;
		public required string merchantSiteId;
		public required string version;
		public string? clientRequestId;
		public required string sessionToken;
		public required string orderId;
		public required string transactionId;
		public required string transactionType;
		public int gwErrorCode;
		public int gwExtendedErrorCode;
		public required PaymentOptionResponseRoot paymentOption;
		public required string customData;
		public string? clientUniqueId;
		public string? userTokenId;
	}
}
