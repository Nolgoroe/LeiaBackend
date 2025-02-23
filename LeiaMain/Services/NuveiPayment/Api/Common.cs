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
		public required string ccCardNumber { get; set; }
		public required string bin { get; set; }
		public required string ccExpMonth { get; set; }
		public required string ccExpYear { get; set; }
		public required string last4Digits { get; set; }
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
