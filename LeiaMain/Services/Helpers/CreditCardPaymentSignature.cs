using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public class CreditCardPaymentSignature
    {
        public static string BuildSignature(
          string companyNum,
          string transType,
          string typeCredit,
          string amount,
          string currency,
          string cardNum,
          string refTransId,
          string personalHashKey
        )
        {
            var raw = $"{companyNum}{transType}{typeCredit}{amount}{currency}{cardNum}{refTransId}{personalHashKey}";

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var b64 = Convert.ToBase64String(hash);
            return b64;
        }
    }
}
