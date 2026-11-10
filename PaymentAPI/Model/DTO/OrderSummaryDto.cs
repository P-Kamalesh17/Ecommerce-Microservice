namespace PaymentAPI.Model.DTO
{
    // Local copy of the fields PaymentAPI needs from OrderAPI's OrderDto.
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
