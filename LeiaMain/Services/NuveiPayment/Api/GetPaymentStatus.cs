namespace Services.NuveiPayment.Api
{
    /// <summary>
    /// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json#getPaymentStatus
    /// </summary>
    [Serializable]
    public class GetPaymentStatusRequest : BaseNuveiApiRequest<GetPaymentStatusResponse>
    {
        public override string[] GetChecksumProperties()
        {
            return [];
        }
    }

    /// <summary>
    /// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json#getPaymentStatus
    /// </summary>
    [Serializable]
    public class GetPaymentStatusResponse : BaseNuveiApiResponse
    {
        public required string transactionType { get; set; }
        public required string transactionId { get; set; }
        public string? userTokenId { get; set; }
        public required string amount { get; set; }
        public required string currency { get; set; }
        public string? authCode { get; set; }
        public string? clientRequestId { get; set; }
        public string? clientUniqueId { get; set; }
        public required PaymentOptionResponseRoot paymentOption { get; set; }
    }
}
