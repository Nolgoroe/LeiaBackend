using Services.DTO;
using Services.Helpers;
using Services.NuveiPayment.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Services.CreditCards
{
    public class CreditCardService
    {
        private const string CompanyNum = "5009523";
        private const string PersonalHashKey = "309NZOP5ZR";
        private const string BaseUrl = "https://process.b2bserve.com/member/remote_charge.asp";

        private readonly HttpClient _httpClient;

        public CreditCardService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<CreditCardPaymentResponse> ChargeAsync(CreditCardChargeRequest req)
        {
            // 1) Build the signature (empty refTransId for a straight SALE)
            var sig = CreditCardPaymentSignature.BuildSignature(
                CompanyNum,
                "0",                   // TransType = SALE
                req.TypeCredit,        // "1" = Debit
                req.Amount.ToString("F2"),
                req.Currency,
                req.CardNum ?? "",
                "",                    // refTransId (none for SALE)
                PersonalHashKey
            );

            // 2) Assemble query parameters
            var parameters = new Dictionary<string, string>
            {
                ["CompanyNum"] = CompanyNum,
                ["TransType"] = "0",
                ["TypeCredit"] = req.TypeCredit,
                ["Payments"] = req.Payments.ToString(),
                ["Amount"] = req.Amount.ToString("F2"),
                ["Currency"] = req.Currency,
                ["CardNum"] = req.CardNum ?? "",
                ["ExpMonth"] = req.ExpMonth,
                ["ExpYear"] = req.ExpYear,
                ["Member"] = req.Member,
                ["CVV2"] = req.CVV,
                ["Email"] = req.Email,
                ["ClientIP"] = req.ClientIP,
                ["Signature"] = sig
            };

            if (!string.IsNullOrEmpty(req.RetURL))
                parameters["RetURL"] = req.RetURL;

            if (!string.IsNullOrEmpty(req.NotificationUrl))
                parameters["notification_url"] = req.NotificationUrl;

            // 3) Build full request URL
            var form = new FormUrlEncodedContent(parameters);
            string qs = await form.ReadAsStringAsync();
            string url = $"{BaseUrl}?{qs}";

            // 4) Execute the request
            var response = await _httpClient.GetAsync(url);
            var rawResponse = await response.Content.ReadAsStringAsync();

            // 5) Parse the "key=value&…" body
            var dict = rawResponse
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('=', 2))
                    .ToDictionary(kv => kv[0],
                                  kv => WebUtility.UrlDecode(kv.Length > 1 ? kv[1] : ""));


            return new CreditCardPaymentResponse
            {
                Reply = dict.GetValueOrDefault("Reply"),
                TransID = dict.GetValueOrDefault("TransID"),
                Date = dict.GetValueOrDefault("Date"),
                Amount = dict.GetValueOrDefault("Amount"),
                Currency = dict.GetValueOrDefault("Currency"),
                CCType = dict.GetValueOrDefault("CCType"),
                Last4 = dict.GetValueOrDefault("Last4"),
                ExpMonth = dict.GetValueOrDefault("ExpMonth"),
                ExpYear = dict.GetValueOrDefault("ExpYear"),
                ReplyDesc = dict.GetValueOrDefault("ReplyDesc"),
                ccBIN = dict.GetValueOrDefault("ccBIN"),
                D3Redirect = dict.GetValueOrDefault("D3Redirect"),
                Needs3DS = dict.GetValueOrDefault("Reply") == "553",
            };
        }


        public async Task<CreditCardStatusByID> GetStatusByIdAsync(string transId)
        {
            var q = new Dictionary<string, string>
            {
                ["CompanyNum"] = CompanyNum,
                ["TransID"] = transId
            };

            // Build GET URL
            var form = new FormUrlEncodedContent(q);
            var url = "https://process.b2bserve.com/member/getStatus.asp?"
                     + await form.ReadAsStringAsync();

            // Call gateway
            var resp = await _httpClient.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync();

            // Parse
            var dict = body
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .ToDictionary(kv => kv[0], kv => WebUtility.UrlDecode(kv[1]));

            return new CreditCardStatusByID
            {
                Reply = dict.GetValueOrDefault("Reply"),
                ReplyDesc = dict.GetValueOrDefault("ReplyDesc"),
                TransID = dict.GetValueOrDefault("TransID")
            };
        }
    }
}
