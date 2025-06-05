using Services.DTO;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    class CreditCardRedirectSignature
    {
        private const string MerchantID = "5009523";
        private const string PersonalHashKey = "309NZOP5ZR";
        private const string BaseUrlRedirect = "https://uiservices.b2bserve.com/hosted/";

        private static string BuildSignatureRaw(
                    string urlRedirect,          // pass raw (no escaping here!)
                    string notificationUrl,      // raw also
                    string transComment,
                    string transRefNum,
                    string Brand,
                    string transInstallments,
                    string amountOptions,
                    string transType,
                    string transAmount,
                    string transCurrency,
                    string dispPaymentType,
                    string dispPayFor,
                    string dispRecurring,
                    string dispLng,
                    string clientFullName,
                    string clientEmail,
                    string clientPhoneNum,
                    string clientIdNum,
                    string clientBillAddress1,
                    string clientBillAddress2,
                    string clientBillCity,
                    string clientBillZipcode,
                    string clientBillState,
                    string clientBillCountry,
                    string dispMobile)
        {
            string eMerchantID = MerchantID; // digits only, no encoding needed
            string eUrlRedirect = urlRedirect;       // “raw,” do NOT WebUtility.UrlEncode here
            string eNotificationUrl = notificationUrl;   // raw, not encoded
            string eTransComment = WebUtility.UrlEncode(transComment);
            string eTransRefNum = transRefNum;
            string eBrand = WebUtility.UrlEncode(Brand);
            string eTransInstallments = WebUtility.UrlEncode(transInstallments);
            string eAmountOptions = WebUtility.UrlEncode(amountOptions);
            string eTransType = WebUtility.UrlEncode(transType);
            string eTransAmount = WebUtility.UrlEncode(transAmount);
            string eTransCurrency = WebUtility.UrlEncode(transCurrency);
            string eDispPaymentType = WebUtility.UrlEncode(dispPaymentType);
            string eDispPayFor = WebUtility.UrlEncode(dispPayFor);
            string eDispRecurring = WebUtility.UrlEncode(dispRecurring);
            string eDispLng = WebUtility.UrlEncode(dispLng);
            string eClientFullName = clientFullName;
            string eClientEmail = clientEmail;
            string eClientPhoneNum = WebUtility.UrlEncode(clientPhoneNum);
            string eClientIdNum = WebUtility.UrlEncode(clientIdNum);
            string eClientBillAddress1 = WebUtility.UrlEncode(clientBillAddress1);
            string eClientBillAddress2 = WebUtility.UrlEncode(clientBillAddress2);
            string eClientBillCity = WebUtility.UrlEncode(clientBillCity);
            string eClientBillZipcode = WebUtility.UrlEncode(clientBillZipcode);
            string eClientBillState = WebUtility.UrlEncode(clientBillState);
            string eClientBillCountry = WebUtility.UrlEncode(clientBillCountry);
            string eDispMobile = WebUtility.UrlEncode(dispMobile);
            string eHashKey = PersonalHashKey;  // raw, no encoding again

            var rawSb = new StringBuilder();
            rawSb.Append(eMerchantID);
            rawSb.Append(eUrlRedirect);
            rawSb.Append(eNotificationUrl);
            rawSb.Append(eTransComment);
            rawSb.Append(eTransRefNum);
            rawSb.Append(eBrand);
            rawSb.Append(eTransInstallments);
            rawSb.Append(eAmountOptions);
            rawSb.Append(eTransType);
            rawSb.Append(eTransAmount);
            rawSb.Append(eTransCurrency);
            rawSb.Append(eDispPaymentType);
            rawSb.Append(eDispPayFor);
            rawSb.Append(eDispRecurring);
            rawSb.Append(eDispLng);
            rawSb.Append(eClientFullName);
            rawSb.Append(eClientEmail);
            rawSb.Append(eClientPhoneNum);
            rawSb.Append(eClientIdNum);
            rawSb.Append(eClientBillAddress1);
            rawSb.Append(eClientBillAddress2);
            rawSb.Append(eClientBillCity);
            rawSb.Append(eClientBillZipcode);
            rawSb.Append(eClientBillState);
            rawSb.Append(eClientBillCountry);
            rawSb.Append(eDispMobile);
            rawSb.Append(eHashKey);

            string raw = rawSb.ToString();

            byte[] hashBytes;
            using (var sha = SHA256.Create())
            {
                hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            }

            string base64 = Convert.ToBase64String(hashBytes);

            return WebUtility.UrlEncode(base64);
        }
        public static string BuildHostedUrl(CreditCardRedirectRequest req)
        {
            // 1) Compute signature using the raw values from req:
            string signature = BuildSignatureRaw(
                urlRedirect: req.UrlRedirect,
                notificationUrl: req.NotificationUrl,
                transComment: req.TransComment,
                transRefNum: req.TransRefNum,
                Brand: req.Brand,
                transInstallments: req.TransInstallments,
                amountOptions: req.AmountOptions,
                transType: req.TransType,
                transAmount: req.TransAmount,
                transCurrency: req.TransCurrency,
                dispPaymentType: req.DispPaymentType,
                dispPayFor: req.DispPayFor,
                dispRecurring: req.DispRecurring,
                dispLng: req.DispLng,
                clientFullName: req.ClientFullName,
                clientEmail: req.ClientEmail,
                clientPhoneNum: req.ClientPhoneNum,
                clientIdNum: req.ClientIdNum,
                clientBillAddress1: req.ClientBillAddress1,
                clientBillAddress2: req.ClientBillAddress2,
                clientBillCity: req.ClientBillCity,
                clientBillZipcode: req.ClientBillZipcode,
                clientBillState: req.ClientBillState,
                clientBillCountry: req.ClientBillCountry,
                dispMobile: req.DispMobile
            );

            // 2) Build the URL query string in the same exact order:

            var sb = new StringBuilder(BaseUrlRedirect);
            sb.Append("?");

            //  1) merchantID
            sb.Append($"merchantID={MerchantID}");

            //  2) url_redirect  (raw, not URL-encoded here)
            sb.Append($"&url_redirect={req.UrlRedirect}");

            //  3) notification_url (raw)
            sb.Append($"&notification_url={req.NotificationUrl}");

            //  4) trans_comment      (URL‐encoded)
            sb.Append($"&trans_comment={WebUtility.UrlEncode(req.TransComment)}");

            //  5) trans_refNum       (URL‐encoded)
            sb.Append($"&trans_refNum={WebUtility.UrlEncode(req.TransRefNum)}");

            //  6) Brand              (URL‐encoded)
            sb.Append($"&Brand={WebUtility.UrlEncode(req.Brand)}");

            //  7) trans_installments (URL‐encoded)
            sb.Append($"&trans_installments={WebUtility.UrlEncode(req.TransInstallments)}");

            //  8) amount_options     (URL‐encoded)
            sb.Append($"&amount_options={WebUtility.UrlEncode(req.AmountOptions)}");

            //  9) trans_type         (URL‐encoded)
            sb.Append($"&trans_type={WebUtility.UrlEncode(req.TransType)}");

            // 10) trans_amount       (URL‐encoded)
            sb.Append($"&trans_amount={WebUtility.UrlEncode(req.TransAmount)}");

            // 11) trans_currency     (URL‐encoded)
            sb.Append($"&trans_currency={WebUtility.UrlEncode(req.TransCurrency)}");

            // 12) disp_paymentType   (URL‐encoded)
            sb.Append($"&disp_paymentType={WebUtility.UrlEncode(req.DispPaymentType)}");

            // 13) disp_payFor        (URL‐encoded)
            sb.Append($"&disp_payFor={WebUtility.UrlEncode(req.DispPayFor)}");

            // 14) disp_recurring     (URL‐encoded)
            sb.Append($"&disp_recurring={WebUtility.UrlEncode(req.DispRecurring)}");

            // 15) disp_lng           (URL‐encoded)
            sb.Append($"&disp_lng={WebUtility.UrlEncode(req.DispLng)}");

            // 16) client_fullName    (URL‐encoded)
            sb.Append($"&client_fullName={WebUtility.UrlEncode(req.ClientFullName)}");

            // 17) client_email       (URL-encoded)
            sb.Append($"&client_email={WebUtility.UrlEncode(req.ClientEmail)}");

            // 18) client_phoneNum    (URL-encoded)
            sb.Append($"&client_phoneNum={WebUtility.UrlEncode(req.ClientPhoneNum)}");

            // 19) client_idNum       (URL-encoded)
            sb.Append($"&client_idNum={WebUtility.UrlEncode(req.ClientIdNum)}");

            // 20) client_billaddress1 (URL-encoded)
            sb.Append($"&client_billaddress1={WebUtility.UrlEncode(req.ClientBillAddress1)}");

            // 21) client_billaddress2 (URL-encoded)
            sb.Append($"&client_billaddress2={WebUtility.UrlEncode(req.ClientBillAddress2)}");

            // 22) client_billcity     (URL-encoded)
            sb.Append($"&client_billcity={WebUtility.UrlEncode(req.ClientBillCity)}");

            // 23) client_billzipcode  (URL-encoded)
            sb.Append($"&client_billzipcode={WebUtility.UrlEncode(req.ClientBillZipcode)}");

            // 24) client_billstate    (URL-encoded)
            sb.Append($"&client_billstate={WebUtility.UrlEncode(req.ClientBillState)}");

            // 25) client_billcountry  (URL-encoded)
            sb.Append($"&client_billcountry={WebUtility.UrlEncode(req.ClientBillCountry)}");

            // 26) disp_mobile         (URL-encoded)
            sb.Append($"&disp_mobile={WebUtility.UrlEncode(req.DispMobile)}");

            // 27) signature           (computed above)
            sb.Append($"&signature={signature}");

            return sb.ToString();
        }    
    }
}
