using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.DTO
{
    public class CreditCardPaymentResponse
    {
        public string Reply { get; set; }
        public string TransID { get; set; }
        public string Date { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string CCType { get; set; }
        public string Last4 { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
        public string ReplyDesc { get; set; }
        public string ccBIN { get; set; }
        public string D3Redirect { get; set; }
        public bool Needs3DS { get; set; }
    }
}
