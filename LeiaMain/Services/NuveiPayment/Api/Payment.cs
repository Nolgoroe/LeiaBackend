namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payment
	/// </summary>
	public class PaymentRequest : BaseNuveiApiRequest<PaymentResponse>
	{
		public string? relatedTransactionId;
		public string? userTokenId;
		public required string amount;
		public required string currency;
		public string? transactionType;
		public required PaymentOptionRoot paymentOption;
		public required DeviceDetails deviceDetails;
		public required BillingAddressDetails billingAddress;
		public DynamicDescriptor? dynamicDescriptor;

		public override string[] GetChecksumProperties()
		{
			return [amount, currency];
		}
	}

	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payment
	/// </summary>
	public class PaymentResponse : BaseNuveiApiResponse
	{
		public int errCode;
		public required string reason;
		public required string merchantId;
		public required string merchantSiteId;
		public required string version;
		public string? clientRequestId;
		public required string sessionToken;
		public required string orderId;
		public required PaymentOptionResponseRoot paymentOption;
		public int gwErrorCode;
		public int gwExtendedErrorCode;
		public required string transactionType;
		public required string transactionId;
		public required string externalTransactionId;
		public required string customData;
		public required FraudDetailsResponse fraudDetails;
		public string? clientUniqueId;
		public string? userTokenId;
	}
}
