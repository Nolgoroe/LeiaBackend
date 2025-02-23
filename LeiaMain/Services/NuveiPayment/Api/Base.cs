
namespace Services.NuveiPayment.Api
{
    [Serializable]
    public abstract class BaseNuveiApiResponse
    {
        public required string status { get; set; }
        public string? transactionStatus { get; set; }
        public required long internalRequestId { get; set; }
        public required string merchantId { get; set; }
        public required string merchantSiteId { get; set; }
        public required string reason { get; set; }
        public int errCode { get; set; }
        public required string version { get; set; }
        public string? sessionToken { get; set; }
    }


    [Serializable]
    public abstract class BaseNuveiApiRequest<T> where T : BaseNuveiApiResponse
    {
        public string clientRequestId { get; } = Guid.NewGuid().ToString();

        public abstract string[] GetChecksumProperties();

    }
}
