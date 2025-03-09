namespace CustomMatching
{
	public static class Constants
	{
		public static readonly IReadOnlyDictionary<string, int> CurrencyCodeToMultiplier = new Dictionary<string, int>
		{
			{ "USD", 1 },
			{ "JPY", 150 },
		};
	}
}
