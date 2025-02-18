namespace Services.NuveiPayment.Api
{

	public class PaymentOptionCard
	{
		public required string cardHolderName;
		public required string cardNumber;
		public required string expirationMonth;
		public required string expirationYear;
		public required string CVV;
	}

	public class PaymentOptionToken
	{
		public required string userTokenId;
	}

	public class PaymentOptionRoot
	{
		public PaymentOptionCard? card;
		public string? userPaymentOptionId;
	}

	public class DeviceDetails
	{
		public required string ipAddress;
		public string? deviceType;
	}

	public class BillingAddressDetails
	{
		public required string country;
		public required string email;
		public required string firstName;
		public required string lastName;
	}

	public class DynamicDescriptor
	{
		public required string merchantName;
		public required string merchantPhone;
	}

	public class UserPaymentOption
	{
		public required string userPaymentOptionId;
	}

	public class PaymentOptionResponseCard
	{
		public required string ccCardNumber;
		public required string bin;
		public required string ccExpMonth;
		public required string ccExpYear;
		public required string last4Digits;
	}

	public class PaymentOptionResponseRoot
	{
		public PaymentOptionResponseCard? card;
		public string? userPaymentOptionId;
	}

	public class FraudDetailsResponse
	{
		public required string finalDecision;
		public required string score;
	}
}
