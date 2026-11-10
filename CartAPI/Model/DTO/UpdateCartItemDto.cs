using System.ComponentModel.DataAnnotations;

namespace CartAPI.Model.DTO
{
    public class UpdateCartItemDto
    {
        [Range(1, 100)]
        public int Quantity { get; set; }
    }
}
