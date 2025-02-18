namespace Services.NuveiPayment.Api
{


    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
    /// </summary>
    [Serializable]
    public class GetSessionTokenRequest : BaseNuveiApiRequest<GetSessionTokenResponse>
    {
        public override string[] GetChecksumProperties()
        {
            return [];
        }
    }

    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
    /// </summary>
    [Serializable]
    public class GetSessionTokenResponse : BaseNuveiApiResponse
    {
        public required string sessionToken;
        public required string internalRequestId;
        public required string status;
        public int errCode;
        public required string reason;
        public required string merchantId;
        public required string merchantSiteId;
        public required string version;
        public string? clientRequestId;
    }
}
