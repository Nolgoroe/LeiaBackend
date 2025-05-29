using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    public class CreditCardChargeRequest
    {
        [Required] public string CardNum { get; set; }
        [Required] public string ExpMonth { get; set; }
        [Required] public string ExpYear { get; set; }
        [Required] public string TypeCredit { get; set; } // "1"=debit
        [Required] public int Payments { get; set; }
        [Required] public decimal Amount { get; set; }
        [Required] public string Currency { get; set; }
        [Required] public string Member { get; set; }
        [Required] public string CVV { get; set; }
        [EmailAddress] public string Email { get; set; }
        [Required] public string ClientIP { get; set; }
        [Url] public string RetURL { get; set; }
        [Url] public string NotificationUrl { get; set; }
    }
}
