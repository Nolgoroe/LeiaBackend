using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
	public class PayoutDetails
	{
		[Key]
		public required Guid PayoutId { get; set; }
		public required Guid WithdrawalId { get; set; }
		public required Guid PlayerId { get; set; }
		public required string UserPaymentOptionId { get; set; }
		public required double Amount { get; set; }
		public required string Processor { get; set; }
		public required string ProcessorTransactionId { get; set; }
		public string? ResponseBody { get; set; }
	}
}
