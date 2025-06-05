using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    public class CreditCardRedirectRequest
    {
        [Required]
        [Url]
        public string UrlRedirect { get; set; }

        [Required]
        [Url]
        public string NotificationUrl { get; set; }

        [Required]
        public string TransRefNum { get; set; }

        [Required]
        public string TransInstallments { get; set; } = "1";

        [Required]
        public string TransType { get; set; } = "0";    // “0” = SALE

        [Required]
        public string TransAmount { get; set; }         // e.g. “500”

        [Required]
        public string TransCurrency { get; set; }       // e.g. “USD”

        [Required]
        public string DispPayFor { get; set; }          // e.g. “Purchase”

        [Required]
        public string DispLng { get; set; }             // e.g. “en-gb”

        [Required]
        public string ClientFullName { get; set; }      // e.g. “Moshe Cohen”

        [Required]
        [EmailAddress]
        public string ClientEmail { get; set; }         // e.g. “moshe@cohen.com”

        [Required]
        public string ClientPhoneNum { get; set; }      // e.g. “1549200456789”


        // Optional (empty if you don’t need them). They must still occupy a “slot.”
        public string TransComment { get; set; } = "";
        public string Brand { get; set; } = "";
        public string AmountOptions { get; set; } = "";
        public string DispPaymentType { get; set; } = "";
        public string DispRecurring { get; set; } = "0";
        public string ClientIdNum { get; set; } = "";
        public string ClientBillAddress1 { get; set; } = "";
        public string ClientBillAddress2 { get; set; } = "";
        public string ClientBillCity { get; set; } = "";
        public string ClientBillZipcode { get; set; } = "";
        public string ClientBillState { get; set; } = "";
        public string ClientBillCountry { get; set; } = "";
        public string DispMobile { get; set; } = "auto";
    }
}

