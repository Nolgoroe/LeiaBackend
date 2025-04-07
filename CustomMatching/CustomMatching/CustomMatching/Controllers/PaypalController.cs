using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class PaymentStatusResponse
{
    public string status { get; set; }
    public string payer_id { get; set; }
}
public class ExecutePaymentRequest
{
    public string payer_id { get; set; }
    public string executeLink { get; set; }
}
public class PayerIdWrapper
{
    public string payer_id { get; set; }
}

public class ExecuteApprovedResponse
{
    public string id { get; set; }
    public string create_time { get; set; }
    public string update_time { get; set; }
    public string state { get; set; }
    public string intent { get; set; }
    public ExecuteApprovedPayer payer { get; set; }
    public List<Transaction> transactions { get; set; }
    public List<Link> links { get; set; }

}
public class VerifyResponse
{
    public string id { get; set; }
    public string intent { get; set; }
    public string state { get; set; }
    public string cart { get; set; }
    public VerifyResponsePayer payer { get; set; }
    public List<Transaction> transactions { get; set; }
    public RedirectUrls redirect_urls { get; set; }
    public string create_time { get; set; }
    public string update_time { get; set; }
    public List<Link> links { get; set; }

}
public class Cart
{
    public string intent { get; set; }
    public CartPayer payer { get; set; }
    public List<Transaction> transactions { get; set; }
    public RedirectUrls redirect_urls { get; set; }
}

public class CartPayer
{
    public string payment_method { get; set; }
}

public class VerifyResponsePayer
{
    public string payment_method { get; set; }
    public string status { get; set; }
    public PayerInfo payer_info { get; set; }

}
public class ExecuteApprovedPayer
{
    public string payment_method { get; set; }
    public PayerInfo payer_info { get; set; }

}
public class PayerInfo
{
    public string email { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string payer_id { get; set; }
    public ShippingAddress shipping_address { get; set; }
    public string country_code { get; set; }
}
public class ShippingAddress
{
    public string recipient_name { get; set; }
    public string line1 { get; set; }
    public string city { get; set; }
    public string state { get; set; }
    public string postal_code { get; set; }
    public string country_code { get; set; }
}

public class Transaction
{
    public Amount amount { get; set; }
    public string invoice_number { get; set; }
}

public class Amount
{
    public string total { get; set; }
    public string currency { get; set; }
}

public class RedirectUrls
{
    public string return_url { get; set; }
    public string cancel_url { get; set; }
}

public class OrderResponse
{
    public string id { get; set; }
    public string state { get; set; }
    public List<Link> links { get; set; }
}

public class Link
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}

public class AccessTokenResponse
{
    public string access_token { get; set; }
    // Add additional fields if present in the actual response
}

[ApiController]
[Route("[controller]")]
public class PaypalController : ControllerBase
{
    HttpClient client;

    private readonly string clientId = "AR5glm_V-ouJTGV9MjscZmVsP5SKewg2x0ajFc3Uo_YfBDEwlIEDtM-Qq1TmLA9CROnmXdipDwzg2bWq";
    private readonly string secretKey = "EJ2JdXZnoEJ10fzWDQJJddLmW_FKTPkcMt3b7YqoOD0bCwMPuCjCQe0tKxKbHkI2BS4hwJ-ybH_FHYZ0";
    private readonly string tokenEndpoint = "https://api-m.paypal.com/v1/oauth2/token";
    private readonly string orderEndpoint = "https://api-m.paypal.com/v1/payments/payment";

    public PaypalController()
    {
        client = new HttpClient();
    }

    [HttpPost("CreatePayment")]
    public async Task<IActionResult> CreatePayment([FromBody] Cart currentCart)
    {
        // 1. Get access token
        var accessToken = await GetAccessToken(clientId, secretKey, tokenEndpoint);
        if (string.IsNullOrEmpty(accessToken))
        {
            return StatusCode(500, "Error obtaining access token from PayPal");
        }

        // 2. Create payment order
        var orderResponse = await CreatePaymentOrder(currentCart, accessToken, orderEndpoint);
        if (orderResponse == null)
        {
            return StatusCode(500, "Error creating payment order");
        }

        // Return the approval URL to the client so that it can redirect the user to PayPal.
        return Ok(orderResponse); //FLAG HARDCODED
    }


    [HttpPost("CheckPaypalPaymentStatus")]
    public async Task<IActionResult> CheckPaypalPaymentStatus([FromBody] string verifyLink)
    {
        var accessToken = await GetAccessToken(clientId, secretKey, tokenEndpoint);
        if (string.IsNullOrEmpty(accessToken))
        {
            return StatusCode(500, "Error obtaining access token from PayPal");
        }

        using (client)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(verifyLink);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error fetching payment details from PayPal");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            // Deserialize into your existing VerifyResponse model.
            var verifyResponse = JsonSerializer.Deserialize<VerifyResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Build a simplified response.
            PaymentStatusResponse statusResponse = new PaymentStatusResponse();
            if (verifyResponse != null && verifyResponse.payer != null)
            {
                statusResponse.status = verifyResponse.payer.status;
                statusResponse.payer_id = verifyResponse.payer.payer_info?.payer_id;
            }
            else
            {
                statusResponse.status = "UNKNOWN";
            }

            return Ok(statusResponse);
        }
    }

    [HttpPost("ExecutePayment")]
    public async Task<IActionResult> ExecutePayment([FromBody] ExecutePaymentRequest request)
    {
        // Get access token (or optionally, cache it until expiration)
        var accessToken = await GetAccessToken(clientId, secretKey, tokenEndpoint);
        if (string.IsNullOrEmpty(accessToken))
        {
            return StatusCode(500, "Error obtaining access token from PayPal");
        }

        var executedResponse = await ExecutePaymentAsync(new PayerIdWrapper { payer_id = request.payer_id }, accessToken, request.executeLink);
        if (executedResponse == null)
        {
            return StatusCode(500, "Error executing payment");
        }

        // You can now trigger any reward logic or transaction logging
        // For example, integrate with your EconomyManager or reward service

        return Ok(executedResponse);
    }



    private async Task<string> GetAccessToken(string clientId, string secretKey, string tokenEndpoint)
    {
        var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{secretKey}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync(tokenEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            // Log error details as needed
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(jsonResponse);
        if (document.RootElement.TryGetProperty("access_token", out JsonElement tokenElement))
        {
            return tokenElement.GetString();
        }

        return null;
    }

    private async Task<OrderResponse> CreatePaymentOrder(Cart currentCart, string accessToken, string orderEndpoint)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var jsonBody = JsonSerializer.Serialize(currentCart);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(orderEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            // Log error details as needed
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OrderResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<object> ExecutePaymentAsync(PayerIdWrapper payerWrapper, string accessToken, string executeLink)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var jsonBody = JsonSerializer.Serialize(payerWrapper);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(executeLink, content);
        if (!response.IsSuccessStatusCode)
        {
            // Log error details as needed
            return null;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        // You may deserialize this into a detailed model (like ExecuteApprovedResponse)
        return JsonSerializer.Deserialize<object>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
