namespace Services.NuveiPayment.Api
{


    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
    /// </summary>
    [Serializable]
    public class GetSessionTokenRequest : BaseNuveiApiRequest<GetSessionTokenResponse>
    {
        public string merchantId;
        public string merchantSiteId;
        public string clientRequestId;

        public override string GetApiUrl(bool isSandbox)
        {
            return isSandbox ?
                "https://ppp-test.nuvei.com/ppp/api/v1/getSessionToken.do" :
                "https://secure.safecharge.com/ppp/api/v1/getSessionToken.do";
        }

        protected override string CreateChecksumString(string merchantSecretKey)
        {
            //checksum order by documentation: merchantId, merchantSiteId, clientRequestId, timeStamp, merchantSecretKey
            return $"{merchantId}{merchantSiteId}{clientRequestId}{timeStamp}{merchantSecretKey}";
        }
    }

    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#getSessionToken
    /// </summary>
    [Serializable]
    public class GetSessionTokenResponse : BaseNuveiApiResponse
    {
        public string sessionToken;
        public string internalRequestId;
        public string status;
        public int errCode;
        public string reason;
        public string merchantId;
        public string merchantSiteId;
        public string version;
        public string clientRequestId;
    }
}
