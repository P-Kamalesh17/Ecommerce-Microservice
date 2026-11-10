using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentAPI.Model
{
    public class Payment
    {
        public int Id { get; set; }

        // The order lives in OrderAPI; one payment per order.
        public int OrderId { get; set; }

        // Copied from the order at creation so ownership checks need no cross-service call.
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(200)]
        public string? TransactionId { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
