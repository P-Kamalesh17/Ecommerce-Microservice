using System.ComponentModel.DataAnnotations;

namespace OrderAPI.Model.DTO
{
    public class CheckoutDto
    {
        // Optional: when empty, the address saved on the user's profile is used.
        [MaxLength(1000)]
        public string? ShippingAddress { get; set; }
    }
}
