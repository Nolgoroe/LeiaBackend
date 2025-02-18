using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.NuveiPayment.Api
{

    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#initPayment
    /// </summary>
    public class InitPaymentResponse : BaseNuveiApiResponse
    {
        public string reason;
        public string orderId;
        public string transactionStatus;
        public string customData;
        public string internalRequestId;
        public string version;
        public string transactionId;
        public string merchantSiteId;
        public string transactionType;
        public int gwExtendedErrorCode;
        public int gwErrorCode;
        public string merchantId;
        public string clientUniqueId;
        public int errCode;
        public string sessionToken;
        public string userTokenId;
        public string status;
        public object paymentOption; // TODO: need to class this
    }

    /// <summary>
    /// http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#initPayment
    /// </summary>
    public class InitPaymentRequest : BaseNuveiApiRequest<InitPaymentResponse>
    {
        public string sessionToken;
        public string merchantId;
        public string merchantSiteId;
        public string userTokenId;
        public string clientUniqueId;
        public string clientRequestId;
        public string amount;
        public string currency;
        public DeviceDetails deviceDetails;

        protected override string CreateChecksumString(string merchantSecretKey)
        {
            return ""; // No checksum needed in the documentation
        }
    }

}
