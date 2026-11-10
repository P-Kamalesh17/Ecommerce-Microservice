namespace PaymentAPI.Model.DTO
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
