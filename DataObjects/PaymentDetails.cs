using System.ComponentModel.DataAnnotations;

namespace DataObjects
{
	public class PaymentDetails
	{
		[Key]
		public required Guid PaymentId { get; set; }
		public required Guid PlayerId { get; set; }
		public required DateTime CreatedAt { get; set; }
		public required double Amount { get; set; }
		public required string CurrencyCode { get; set; }
		public required string PaymentPackageId { get; set; }
		public required string Status { get; set; }
		// The transaction ID will be the Nuvei's sessionToken until the status = Success
		public required string ProcessorTransactionId { get; set; }
		public string? SimplyConnectResponse { get; set; }
		public string? ResponseBody { get; set; }
	}
}
