using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
	public class WithdrawalDetails
	{
		[Key]
		public required Guid WithdrawalId { get; set; }
		public required Guid PlayerId { get; set; }
		public required string Status { get; set; }
		public required double Amount { get; set; }
		public required string CurrencyCode { get; set; }
		public required DateTime CreatedAt { get; set; }
		public DateTime? ProcessedAt { get; set; }
		public required Guid MutationToken { get; set; }
	}
}
