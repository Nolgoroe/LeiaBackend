namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payment
	/// </summary>
	public class PaymentRequest : BaseNuveiApiRequest<PaymentResponse>
	{
		public string? relatedTransactionId { get; set; }
		public string? userTokenId { get; set; }
		public required string amount { get; set; }
		public required string currency { get; set; }
		public string? transactionType { get; set; }
		public required PaymentOptionRoot paymentOption { get; set; }
		public required DeviceDetails deviceDetails { get; set; }
		public required BillingAddressDetails billingAddress { get; set; }
		public DynamicDescriptor? dynamicDescriptor { get; set; }

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
		public string? clientRequestId { get; set; }
		public required string orderId { get; set; }
		public required PaymentOptionResponseRoot paymentOption { get; set; }
		public int gwErrorCode { get; set; }
		public int gwExtendedErrorCode { get; set; }
		public required string transactionType { get; set; }
		public required string transactionId { get; set; }
		public required string externalTransactionId { get; set; }
		public required string customData { get; set; }
		public FraudDetailsResponse? fraudDetails { get; set; }
		public string? clientUniqueId { get; set; }
		public string? userTokenId { get; set; }
	}
}
