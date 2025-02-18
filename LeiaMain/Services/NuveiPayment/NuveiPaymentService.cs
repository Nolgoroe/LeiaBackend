using System.Text;
using Services.NuveiPayment.Api;

namespace Services.NuveiPayment
{

    public interface INuveiPaymentService
    {
        Task<string> ProcessPaymentAsync(string transactionId, decimal amount, string currency);
    }

    public class NuveiPaymentService : INuveiPaymentService
    {
        private const string PRODUCTION_CHARGE_API_URL = "https://secure.safecharge.com/ppp/api/v1/payment.do";
        private const string TEST_CHARGE_API_URL = "https://ppp-test.nuvei.com/ppp/api/v1/payment.do";
        private readonly string _merchantId = "YourMerchantID";
        private readonly string _merchantSiteId = "YourMerchantSiteID";
        private readonly string _apiKey = "YourApiKey";
        private readonly string _secretKey = "YourSecretKey";
        private readonly HttpClient _httpClient;
        private readonly bool _isSandbox = true;

        public NuveiPaymentService()
        {
            _httpClient = new HttpClient();
        }


        private async Task<T> PerformHttpPost<T>(BaseNuveiApiRequest<T> request) where T : BaseNuveiApiResponse
        {
            request.PrepareAndChecksum(_secretKey);
            var url = request.GetApiUrl(_isSandbox);
            var jsonRequest = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync(url, content);

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var response = System.Text.Json.JsonSerializer.Deserialize<T>(jsonResponse);
            if (response == null)
            {
                throw new Exception($"Could not deserialize json response from {url}");
            }
            return response;
        }

        private async Task<GetSessionTokenResponse> GetSessionToken(string clientRequestId)
        {
            var request = new GetSessionTokenRequest()
            {
                merchantId = _merchantId,
                merchantSiteId = _merchantSiteId,
                clientRequestId = clientRequestId,
            };
            return await PerformHttpPost(request);
            
        }


        public async Task<string> ProcessPaymentAsync(string transactionId, decimal amount, string currency)
        {
            var requestUri = GetApiUrlForPayments();

            var timestamp = DateTime.Now.ToString("YYYYMMDDHHmmss");
            var clientRequestId = transactionId;
            /*
                * The hashed values (SHA-256 encoded) of the input parameters, which are concatenated in the following order: merchantId, merchantSiteId, clientRequestId, amount, currency, timeStamp, merchantSecretKey
                */

            var hashString = $"{_merchantId}{_merchantSiteId}{clientRequestId}{amount}{currency}{timestamp}{_secretKey}";
            var checksum = NuveiUtils.HashSha256(hashString);
            // Read docs at http://docs.nuvei.com/api/main/indexMain_v1_0.html?json=#payment
            var requestBody = new
            {
                merchantId = _merchantId,
                merchantSiteId = _merchantSiteId,
                clientRequestId = transactionId,
                sessionToken = "",
                amount = amount,
                currency = currency,
                checksum = checksum,
                timeStamp = timestamp,
                billingAddress = new
                {
                    country = "IL",
                    email = "hello@leia.games",
                    firstName = "Leia",
                    lastName = "Games",
                },
                deviceDetails = new
                {
                    ipAddress = "0.0.0.0",
                    deviceType = "SMARTPHONE",
                },
                paymentOption = new
                {
                    card = new
                    {
                        cardNumber = "4111111111111111",
                        cardHolderName = "John Doe",
                        expirationMonth = "12",
                        expirationYear = "2030",
                        CVV = "123"
                    }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, content);

            return await response.Content.ReadAsStringAsync();
        }

        private string GetApiUrlForPayments() => _isSandbox ? TEST_CHARGE_API_URL : PRODUCTION_CHARGE_API_URL;
    }
}

