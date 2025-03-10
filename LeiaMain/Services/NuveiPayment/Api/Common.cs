using System.Text.Json;
using System.Text.Json.Nodes;

namespace Services.NuveiPayment.Api
{

	public class PaymentOptionCard
	{
		public required string cardHolderName { get; set; }
		public required string cardNumber { get; set; }
		public required string expirationMonth { get; set; }
		public required string expirationYear { get; set; }
		public required string CVV { get; set; }
	}

	public class PaymentOptionToken
	{
		public required string userTokenId { get; set; }
	}

	public class PaymentOptionRoot
	{
		public PaymentOptionCard? card { get; set; }
		public string? userPaymentOptionId { get; set; }
	}

	public class DeviceDetails
	{
		public required string ipAddress { get; set; }
		public string? deviceType { get; set; }
	}

	public class BillingAddressDetails
	{
		public required string country { get; set; }
		public required string email { get; set; }
		public required string firstName { get; set; }
		public required string lastName { get; set; }

		public JsonNode ToJsonNode()
		{
			string jsonString = JsonSerializer.Serialize(this);
			return JsonNode.Parse(jsonString)!;
		}
	}

	public class DynamicDescriptor
	{
		public required string merchantName { get; set; }
		public required string merchantPhone { get; set; }
	}

	public class UserPaymentOption
	{
		public required string userPaymentOptionId { get; set; }
	}

	public class PaymentOptionResponseCard
	{
		public string? ccCardNumber { get; set; }
		public string? bin { get; set; }
		public string? ccExpMonth { get; set; }
		public string? ccExpYear { get; set; }
		public string? last4Digits { get; set; }
		public string? uniqueCC { get; set; }
	}

	public class PaymentOptionResponseRoot
	{
		public PaymentOptionResponseCard? card { get; set; }
		public string? userPaymentOptionId { get; set; }
	}

	public class FraudDetailsResponse
	{
		public required string finalDecision { get; set; }
		public required string score { get; set; }
	}
}
