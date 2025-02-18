
namespace Services.NuveiPayment.Api
{
    [Serializable]
    public abstract class BaseNuveiApiResponse
    {

    }


    [Serializable]
    public abstract class BaseNuveiApiRequest<T> where T : BaseNuveiApiResponse
    {
        public readonly string clientRequestId = Guid.NewGuid().ToString();

        public abstract string[] GetChecksumProperties();

    }
}
