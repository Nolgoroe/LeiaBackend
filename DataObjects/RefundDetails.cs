using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
	public class RefundDetails
	{
		[Key]
		public required Guid RefundId { get; set; }
		public required Guid WithdrawalId { get; set; }
		public required Guid PlayerId { get; set; }
		public required Guid PaymentId { get; set; }
		public required double Amount { get; set; }
		public required string Processor { get; set; }
		public required string ProcessorTransactionId { get; set; }
		public string? ResponseBody { get; set; }
	}
}
