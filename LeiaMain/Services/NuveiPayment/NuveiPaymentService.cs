using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Services.NuveiPayment.Api;

namespace Services.NuveiPayment
{

    public interface INuveiPaymentService
    {
        // Get signed URL for tokenizing the credit-card information
        Task<string> ProcessPaymentWithCardDetailsAsync(decimal amount, string currency, Boolean? useInitPayment);
        Task<string> ProcessPaymentWithTokenAsync(string userId, string userPaymentOptionId, decimal amount, string currency);
        Task<string> ProcessRefundAsync(string nuveiPaymentId, decimal amount, string currency);
        Task<string> ProcessPayoutAsync(string userId, string userPaymentOptionId, decimal amount, string currency);
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

        private async Task<RefundResponse> RefundAsync(string nuveiPaymentId, decimal amount, string currency, string sessionToken)
        {
            var request = new RefundRequest()
            {
                amount = amount.ToString(),
                currency = currency,
                relatedTransactionId = nuveiPaymentId,
            };
            return await PerformHttpPost(request, "refundTransaction", sessionToken);
        }

        private async Task<PayoutResponse> PayoutAsync(string userId, string userPaymentOptionId, decimal amount, string currency, string sessionToken)
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

        public async Task<string> ProcessPaymentWithCardDetailsAsync(decimal amount, string currency, Boolean? useInitPayment)
        {
            var initPaymentRequest = new InitPaymentRequest()
            {
                amount = amount.ToString(),
                currency = currency,
                paymentOption = new PaymentOptionRoot
                {
                    card = new PaymentOptionCard
                    {
                        cardNumber = "4111111111111111",
                        cardHolderName = "John Doe",
                        expirationMonth = "12",
                        expirationYear = "2030",
                        CVV = "123"
                    }
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

            var paymentResponse = await PaymentAsync(initPaymentRequest, sessionToken, relatedTransactionId);
            NuveiUtils.AssertValidResponse(paymentResponse);

            return paymentResponse.transactionId;
        }

        public async Task<string> ProcessPaymentWithTokenAsync(string userId, string userPaymentOptionId, decimal amount, string currency)
        {
            var initPaymentRequest = new InitPaymentRequest()
            {
                userTokenId = userId,
                amount = amount.ToString(),
                currency = currency,
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
            var initPaymentResponse = await InitPaymentAsync(initPaymentRequest, sessionToken);
            string relatedTransactionId = initPaymentResponse.transactionId;

            var paymentResponse = await PaymentAsync(initPaymentRequest, relatedTransactionId, sessionToken);
            return paymentResponse.transactionId;
        }

        public async Task<string> ProcessRefundAsync(string nuveiPaymentId, decimal amount, string currency)
        {
            string sessionToken = await GetSessionToken();
            var response = await RefundAsync(nuveiPaymentId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);
            return response.transactionId;
        }

        public async Task<string> ProcessPayoutAsync(string userId, string userPaymentOptionId, decimal amount, string currency)
        {
            string sessionToken = await GetSessionToken();
            var response = await PayoutAsync(userId, userPaymentOptionId, amount, currency, sessionToken);
            NuveiUtils.AssertValidResponse(response);
            return response.transactionId;
        }
    }
}

