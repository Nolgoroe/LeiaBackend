namespace Services.NuveiPayment.Api
{


    /// <summary>
    /// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
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
    /// https://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
    /// </summary>
    [Serializable]
    public class GetSessionTokenResponse : BaseNuveiApiResponse
    {
        public string? clientRequestId { get; set; }
    }
}
