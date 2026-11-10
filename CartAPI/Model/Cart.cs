using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
