
namespace Services.NuveiPayment.Api
{
    [Serializable]
    public abstract class BaseNuveiApiResponse
    {

    }


    [Serializable]
    public abstract class BaseNuveiApiRequest<T> where T : BaseNuveiApiResponse
    {
        private const string NUVEI_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";

        public string timeStamp;
        public string checksum;

        /// <summary>
        /// Many API requests demand a timeStamp and checksum parameters, each request hashes different params in different order
        /// This is a helper that creates the checksum and timestamp, it must be called before sending the request
        /// </summary>
        /// <param name="merchantSecretKey"></param>
        public void PrepareAndChecksum(string merchantSecretKey)
        {
            timeStamp = DateTime.Now.ToString(NUVEI_TIMESTAMP_FORMAT);
            checksum = NuveiUtils.HashSha256(CreateChecksumString(merchantSecretKey));
        }

        public abstract string GetApiUrl(bool isSandbox);

        protected abstract string CreateChecksumString(string merchantSecretKey);

    }
}
