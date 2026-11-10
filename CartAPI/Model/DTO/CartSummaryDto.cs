namespace CartAPI.Model.DTO
{
    public class CartSummaryDto
    {
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
