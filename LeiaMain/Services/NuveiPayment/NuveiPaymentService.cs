using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Services.NuveiPayment.Api;

namespace Services.NuveiPayment
{

    public interface INuveiPaymentService
    {
        Task<PaymentResponse> ProcessPaymentWithCardDetailsAsync(PaymentOptionCard card, double amount, int currencyId, Boolean? useInitPayment);
        Task<PaymentResponse> ProcessPaymentWithTokenAsync(Guid userId, string userPaymentOptionId, double amount, int currencyId, Boolean? useInitPayment);
        Task<RefundResponse> ProcessRefundAsync(string nuveiPaymentId, double amount, int currencyId);
        Task<PayoutResponse> ProcessPayoutAsync(string userId, string userPaymentOptionId, double amount, int currencyId);
    }

    public class NuveiPaymentService : INuveiPaymentService
    {
        private readonly string _apiBaseUrl;
        private readonly string _merchantId;
        private readonly string _merchantSiteId;
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;
        private const string NUVEI_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";

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

        /// <summary>
        /// Nuvei API requests demand a checksum parameter. Each request hashes different params in different order.
        /// This is a helper that creates the checksum. It must be called before sending the request.
        /// https://docs.nuvei.com/documentation/features/authentication/#Hashing_Calculation_the_checksum_parameter
        /// </summary>
        private string GenerateRequestChecksum<T>(BaseNuveiApiRequest<T> request, string timeStamp) where T : BaseNuveiApiResponse
        {
            string requestChecksumProperties = string.Join("", request.GetChecksumProperties());
            string checksumBase = $"{_merchantId}{_merchantSiteId}{request.clientRequestId}{requestChecksumProperties}{timeStamp}{_secretKey}";
            return NuveiUtils.HashSha256(checksumBase);
        }

        private StringContent GetRequestHttpContent<T>(BaseNuveiApiRequest<T> request, string? sessionToken) where T : BaseNuveiApiResponse
        {
            string timeStamp = DateTime.Now.ToString(NUVEI_TIMESTAMP_FORMAT);
            string checksum = GenerateRequestChecksum(request, timeStamp);

            var jsonObj = JsonNode.Parse(JsonSerializer.Serialize(request, request.GetType()))!.AsObject();
            jsonObj["merchantId"] = _merchantId;
            jsonObj["merchantSiteId"] = _merchantSiteId;
            jsonObj["timeStamp"] = timeStamp;
            jsonObj["checksum"] = checksum;
            if (sessionToken is not null)
            {
                jsonObj["sessionToken"] = sessionToken;
            }

            return new StringContent(jsonObj.ToJsonString(), Encoding.UTF8, "application/json");
        }

        private async Task<T> PerformHttpPost<T>(BaseNuveiApiRequest<T> request, string endpoint, string? sessionToken) where T : BaseNuveiApiResponse
        {
            var url = $"{_apiBaseUrl}/{endpoint}.do";
            var content = GetRequestHttpContent(request, sessionToken);
            var httpResponse = await _httpClient.PostAsync(url, content);
            httpResponse.EnsureSuccessStatusCode();

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<T>(jsonResponse);
            if (response is null)
            {
                throw new Exception($"Could not deserialize json response from \"{url}\"");
            }
            if (response?.status != "SUCCESS")
            {
                throw new Exception($"Nuvei API response status is not Success; \"{response?.status}\"");
            }

            return response;
        }

        private string FormatPaymentAmount(double amount)
        {
            return Convert.ToInt32(Math.Floor(amount * 100)).ToString();
        }

        private async Task<string> GetSessionToken()
        {
            var request = new GetSessionTokenRequest();
            var response = await PerformHttpPost(request, "getSessionToken", null);
            if (string.IsNullOrWhiteSpace(response?.sessionToken))
            {
                throw new Exception("Nuvei getSessionToken response did not include a session token");
            }

            return response.sessionToken;
        }

        private async Task<InitPaymentResponse> InitPaymentAsync(InitPaymentRequest request, string sessionToken)
        {
            return await PerformHttpPost(request, "initPayment", sessionToken);
        }

        private async Task<PaymentResponse> PaymentAsync(InitPaymentRequest initPaymentRequest, string sessionToken, string relatedTransactionId)
        {
            var request = new PaymentRequest
            {
                amount = initPaymentRequest.amount,
                currency = initPaymentRequest.currency,
                paymentOption = initPaymentRequest.paymentOption,
                deviceDetails = initPaymentRequest.deviceDetails,
                billingAddress = initPaymentRequest.billingAddress,
                transactionType = "Sale",
            };
            if (relatedTransactionId != "")
            {

                request.relatedTransactionId = relatedTransactionId;
            }
            if (initPaymentRequest.userTokenId is not null)
            {
                request.userTokenId = initPaymentRequest.userTokenId;
            }
            return await PerformHttpPost(request, "payment", sessionToken);
        }

        private async Task<RefundResponse> RefundAsync(string nuveiPaymentId, double amount, string currency, string sessionToken)
        {
            var request = new RefundRequest()
            {
                amount = amount.ToString(),
                currency = currency,
                relatedTransactionId = nuveiPaymentId,
            };
            return await PerformHttpPost(request, "refundTransaction", sessionToken);
        }

        private async Task<PayoutResponse> PayoutAsync(string userId, string userPaymentOptionId, double amount, string currency, string sessionToken)
        {
            var request = new PayoutRequest()
            {
                userTokenId = userId,
                amount = amount.ToString(),
                currency = currency,
                userPaymentOption = new UserPaymentOption
                {
                    userPaymentOptionId = userPaymentOptionId,
                },
                deviceDetails = new DeviceDetails
                {
                    ipAddress = "0.0.0.0",
                    deviceType = "SMARTPHONE"
                }
            };
            return await PerformHttpPost(request, "payout", sessionToken);
        }

        private string GetCurrencyCodeFromCurrencyId(int currencyId)
        {
            // TODO: implement
            return "USD";
        }

        public async Task<PaymentResponse> ProcessPaymentWithCardDetailsAsync(PaymentOptionCard card, double amount, int currencyId, Boolean? useInitPayment)
        {
            var initPaymentRequest = new InitPaymentRequest()
            {
                amount = FormatPaymentAmount(amount),
                currency = GetCurrencyCodeFromCurrencyId(currencyId),
                paymentOption = new PaymentOptionRoot { card = card, },
                billingAddress = new BillingAddressDetails
                {
                    country = "IL",
                    email = "hello@leia.games",
                    firstName = "Leia",
                    lastName = "Games",
                },
                deviceDetails = new DeviceDetails
                {
                    ipAddress = "0.0.0.0",
                    deviceType = "SMARTPHONE",
                },
            };

            string sessionToken = await GetSessionToken();
            string relatedTransactionId = "";
            if (useInitPayment == true)
            {
                var initPaymentResponse = await InitPaymentAsync(initPaymentRequest, sessionToken);
                NuveiUtils.AssertValidResponse(initPaymentResponse);

                relatedTransactionId = initPaymentResponse.transactionId;
            }

            PaymentResponse paymentResponse = await PaymentAsync(initPaymentRequest, sessionToken, relatedTransactionId);
            NuveiUtils.AssertValidResponse(paymentResponse);

            return paymentResponse;
        }

        public async Task<PaymentResponse> ProcessPaymentWithTokenAsync(Guid userId, string userPaymentOptionId, double amount, int currencyId, Boolean? useInitPayment)
        {
            var initPaymentRequest = new InitPaymentRequest()
            {
                userTokenId = userId.ToString(),
                amount = FormatPaymentAmount(amount),
                currency = GetCurrencyCodeFromCurrencyId(currencyId),
                paymentOption = new PaymentOptionRoot
                {
                    userPaymentOptionId = userPaymentOptionId,
                },
                billingAddress = new BillingAddressDetails
                {
                    country = "IL",
                    email = "hello@leia.games",
                    firstName = "Leia",
                    lastName = "Games",
                },
                deviceDetails = new DeviceDetails
                {
                    ipAddress = "0.0.0.0",
                    deviceType = "SMARTPHONE",
                },
            };

            string sessionToken = await GetSessionToken();
            string relatedTransactionId = "";
            if (useInitPayment == true)
            {
                var initPaymentResponse = await InitPaymentAsync(initPaymentRequest, sessionToken);
                NuveiUtils.AssertValidResponse(initPaymentResponse);

                relatedTransactionId = initPaymentResponse.transactionId;
            }

            PaymentResponse paymentResponse = await PaymentAsync(initPaymentRequest, relatedTransactionId, sessionToken);
            NuveiUtils.AssertValidResponse(paymentResponse);

            return paymentResponse;
        }

        public async Task<RefundResponse> ProcessRefundAsync(string nuveiPaymentId, double amount, int currencyId)
        {
            string sessionToken = await GetSessionToken();
            string currency = GetCurrencyCodeFromCurrencyId(currencyId);
            RefundResponse response = await RefundAsync(nuveiPaymentId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);

            return response;
        }

        public async Task<PayoutResponse> ProcessPayoutAsync(string userId, string userPaymentOptionId, double amount, int currencyId)
        {
            string sessionToken = await GetSessionToken();
            string currency = GetCurrencyCodeFromCurrencyId(currencyId);
            PayoutResponse response = await PayoutAsync(userId, userPaymentOptionId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);

            return response;
        }
    }
}

