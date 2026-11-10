namespace DAL.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int CartId { get; set; }

        public int ProductId { get; set; }

        public Cart Cart { get; set; } = null!;
    }
}
