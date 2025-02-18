using System.Security.Cryptography;
using System.Text;
using Services.NuveiPayment.Api;

namespace Services.NuveiPayment
{
    public static class NuveiUtils
    {

        private static readonly SHA256 _sha256 = SHA256.Create();

        public static string HashSha256(string input)
        {
            byte[] bytes = _sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public static void AssertValidResponse(BaseNuveiApiResponse response)
        {
            if (response?.transactionStatus != "Approved")
            {
                throw new Exception($"Nuvei transactionStatus is not Approved; \"{response?.transactionStatus}\"");
            }
        }
    }
}
