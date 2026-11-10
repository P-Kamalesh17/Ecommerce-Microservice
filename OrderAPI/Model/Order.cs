using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderAPI.Model
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        [MaxLength(1000)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
