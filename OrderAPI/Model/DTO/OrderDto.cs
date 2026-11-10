namespace OrderAPI.Model.DTO
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Only filled by GET my-orders. A null PaymentId means no payment has been made yet (still Pending).
        public int? PaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
