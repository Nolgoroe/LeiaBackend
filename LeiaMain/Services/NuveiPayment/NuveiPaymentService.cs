using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Services.NuveiPayment.Api;

namespace Services.NuveiPayment
{

    public interface INuveiPaymentService
    {
        Task<JsonObject> GetIframeCheckoutJsonObject(string userId, string clientUniqueId, double amount, string currencyCode, BillingAddressDetails billingAddressDetails);
        Task<GetPaymentStatusResponse> GetPaymentStatus(string sessionToken);
        Task<PaymentResponse> ProcessPaymentWithTokenAsync(Guid userId, string userPaymentOptionId, double amount, string currencyCode, Boolean? useInitPayment);
        Task<RefundResponse> ProcessRefundAsync(string nuveiPaymentId, double amount, string currencyCode);
        Task<PayoutResponse> ProcessPayoutAsync(string userId, string userPaymentOptionId, double amount, string currencyCode);
    }

    public class NuveiPaymentService : INuveiPaymentService
    {
        private readonly string _apiBaseUrl;
        private readonly string _merchantId;
        private readonly string _merchantSiteId;
        private readonly string _secretKey;
        private readonly HttpClient _httpClient;
        private const string NUVEI_TIMESTAMP_FORMAT = "yyyyMMddHHmmss";

        public NuveiPaymentService(string apiBaseUrl, string merchantId, string merchantSiteId, string secretKey)
        {
            _httpClient = new HttpClient();
            _apiBaseUrl = apiBaseUrl;
            _merchantId = merchantId;
            _merchantSiteId = merchantSiteId;
            _secretKey = secretKey;
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

        private async Task<T> PerformHttpPost<T>(BaseNuveiApiRequest<T> request, string endpoint, string? sessionToken, Boolean includeChecksum = true) where T : BaseNuveiApiResponse
        {
            var url = $"{_apiBaseUrl}/{endpoint}.do";
            var content = includeChecksum
                ? GetRequestHttpContent(request, sessionToken)
                : new StringContent(new JsonObject { ["sessionToken"] = sessionToken }.ToJsonString(), Encoding.UTF8, "application/json");
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

        private async Task<OpenOrderResponse> OpenOrderAsync(OpenOrderRequest request, string sessionToken)
        {
            return await PerformHttpPost(request, "openOrder", sessionToken);
        }

        private async Task<GetPaymentStatusResponse> GetPaymentStatusAsync(string sessionToken)
        {
            GetPaymentStatusRequest request = new GetPaymentStatusRequest();
            return await PerformHttpPost(request, "getPaymentStatus", sessionToken, false);
        }

        private async Task<InitPaymentResponse> InitPaymentAsync(InitPaymentRequest request, string sessionToken)
        {
            return await PerformHttpPost(request, "initPayment", sessionToken);
        }

        private async Task<PaymentResponse> PaymentAsync(InitPaymentRequest initPaymentRequest, string relatedTransactionId, string sessionToken)
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

        public async Task<JsonObject> GetIframeCheckoutJsonObject(string userId, string clientUniqueId, double amount, string currencyCode, BillingAddressDetails billingAddressDetails)
        {
            string sessionToken = await GetSessionToken();
            OpenOrderRequest request = new OpenOrderRequest
            {
                clientUniqueId = clientUniqueId,
                currency = currencyCode,
                amount = FormatPaymentAmount(amount),
                userTokenId = userId,
            };
            await OpenOrderAsync(request, sessionToken);
            // PerformHttpPost already asserts "success" = true

            var returnDict = new JsonObject
            {
                { "sessionToken", sessionToken },
                { "merchantId", _merchantId },
                { "merchantSiteId", _merchantSiteId },
                { "amount", amount },
                { "currency", currencyCode },
                { "country", currencyCode == "JPY" ? "JP" : "US" },
                { "billingAddress", billingAddressDetails.ToJsonNode() },
                { "savePM", "force" },
                { "alwaysCollectCvv", true }
            };

            return returnDict;
        }

        public async Task<GetPaymentStatusResponse> GetPaymentStatus(string sessionToken)
        {
            return await GetPaymentStatusAsync(sessionToken);
        }

        public async Task<PaymentResponse> ProcessPaymentWithTokenAsync(Guid userId, string userPaymentOptionId, double amount, string currencyCode, Boolean? useInitPayment)
        {
            var initPaymentRequest = new InitPaymentRequest()
            {
                userTokenId = userId.ToString(),
                amount = FormatPaymentAmount(amount),
                currency = currencyCode,
                paymentOption = new PaymentOptionRoot
                {
                    userPaymentOptionId = userPaymentOptionId,
                    // TODO: Ask and include the CVV in the flow unless Nuvei agrees to remove the requirement
                    // card = new PaymentOptionCard { CVV = "123", }
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

        public async Task<RefundResponse> ProcessRefundAsync(string nuveiPaymentId, double amount, string currencyCode)
        {
            string sessionToken = await GetSessionToken();
            string currency = currencyCode;
            RefundResponse response = await RefundAsync(nuveiPaymentId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);

            return response;
        }

        public async Task<PayoutResponse> ProcessPayoutAsync(string userId, string userPaymentOptionId, double amount, string currencyCode)
        {
            string sessionToken = await GetSessionToken();
            string currency = currencyCode;
            PayoutResponse response = await PayoutAsync(userId, userPaymentOptionId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);

            return response;
        }
    }
}

