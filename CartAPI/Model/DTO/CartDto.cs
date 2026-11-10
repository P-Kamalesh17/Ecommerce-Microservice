namespace CartAPI.Model.DTO
{
    public class CartDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }
}
