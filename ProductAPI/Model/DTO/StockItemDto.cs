using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Model.DTO
{
    public class StockItemDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
