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
        private readonly string _apiBaseUrl;
        private readonly string _merchantId;
        private readonly string _merchantSiteId;
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;

        public NuveiPaymentService()
        {
            _httpClient = new HttpClient();

            _apiBaseUrl = GetRequiredEnvironmentVariable("NUVEI_API_BASE_URL");
            _merchantId = GetRequiredEnvironmentVariable("NUVEI_MERCHANT_ID");
            _merchantSiteId = GetRequiredEnvironmentVariable("NUVEI_MERCHANT_SITE_ID");
            _secretKey = GetRequiredEnvironmentVariable("NUVEI_SECRET_KEY");
        }

        private static string GetRequiredEnvironmentVariable(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Environment variable {key} is not set or is empty.");
            }
            return value;
        }

        private async Task<T> PerformHttpPost<T>(BaseNuveiApiRequest<T> request, string endpoint) where T : BaseNuveiApiResponse
        {
            request.PrepareAndChecksum(_secretKey);
            var url = $"{_apiBaseUrl}/{endpoint}.do";
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
            return await PerformHttpPost(request, "getSessionToken");
        }


        public async Task<string> ProcessPaymentAsync(string transactionId, decimal amount, string currency)
        {
            var requestUri = $"{_apiBaseUrl}/payment.do";

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
                amount,
                currency,
                checksum,
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
    }
}

