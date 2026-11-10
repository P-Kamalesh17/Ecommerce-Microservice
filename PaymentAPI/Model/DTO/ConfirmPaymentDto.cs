using System.ComponentModel.DataAnnotations;

namespace PaymentAPI.Model.DTO
{
    public class ConfirmPaymentDto
    {
        [Range(1, int.MaxValue)]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TransactionId { get; set; } = string.Empty;
    }
}
