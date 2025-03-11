namespace Services.NuveiPayment.Api
{
	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json#openOrder
	/// </summary>
	public class OpenOrderRequest : BaseNuveiApiRequest<OpenOrderResponse>
	{
		public required string clientUniqueId { get; set; }
		public required string userTokenId { get; set; }
		public required string amount { get; set; }
		public required string currency { get; set; }
		public string? preventOverride { get; set; }
		public string? transactionType { get; set; }
		public BillingAddressDetails? billingAddress { get; set; }

		public override string[] GetChecksumProperties()
		{
			return [amount, currency];
		}
	}

	/// <summary>
	/// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json#openOrder
	/// </summary>
	public class OpenOrderResponse : BaseNuveiApiResponse
	{
		public string? clientRequestId { get; set; }
		public string? clientUniqueId { get; set; }
		public required string userTokenId { get; set; }
		public double? orderId { get; set; }
	}
}
