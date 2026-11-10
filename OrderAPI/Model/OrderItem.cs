using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderAPI.Model
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        // Snapshot taken at checkout: the product lives in another service.
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        public Order Order { get; set; } = null!;
    }
}
