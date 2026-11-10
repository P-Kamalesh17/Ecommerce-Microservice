namespace OrderAPI.Model.DTO
{
    // Local copies of the shapes returned by CartAPI (GET api/cart/{userId}).
    public class CartDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}
