namespace CustomMatching
{
	public class PaymentPackage
	{
		public string ID { get; }
		public int AmountInUsd { get; }
		public int Gems { get; }
		public double BonusAmount { get; }

		public PaymentPackage(string id, int amountInUsd, int gems, double bonusAmount)
		{
			ID = id;
			AmountInUsd = amountInUsd;
			Gems = gems;
			BonusAmount = bonusAmount;
		}
	}

	public static class Constants
	{
		public static readonly IReadOnlyDictionary<string, int> CurrencyCodeToMultiplier = new Dictionary<string, int>
		{
			{ "USD", 1 },
			{ "JPY", 150 },
		};

		public static readonly Dictionary<string, PaymentPackage> PaymentPackages = new Dictionary<string, PaymentPackage>
		{
			{ "Basic1", new PaymentPackage("Basic1", 5, 50, 0.0) },
			{ "Basic2", new PaymentPackage("Basic2", 10, 200, 0.5) },
			{ "Basic3", new PaymentPackage("Basic3", 15, 350, 2.1) },
			{ "Basic4", new PaymentPackage("Basic4", 25, 600, 4.0) },
			{ "Basic5", new PaymentPackage("Basic5", 35, 900, 6.3) },
			{ "Basic6", new PaymentPackage("Basic6", 50, 1500, 12.5) },
			{ "StarterPack", new PaymentPackage("StarterPack", 10, 600, 5.0) },
			{ "Limitedoffer", new PaymentPackage("Limitedoffer", 5, 400, 0.9) },
			{ "TaichuStar", new PaymentPackage("TaichuStar", 12, 300, 1.8) },
			{ "Especiallyforyou", new PaymentPackage("Especiallyforyou", 3, 60, 0.3) },
			{ "EspeciallyforyouF2P", new PaymentPackage("EspeciallyforyouF2P", 3, 300, 0.0) },
		};
	}
}
