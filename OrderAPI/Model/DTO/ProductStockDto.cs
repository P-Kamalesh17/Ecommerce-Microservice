namespace OrderAPI.Model.DTO
{
    // Local copy of the fields OrderAPI needs from ProductAPI's ProductDto.
    public class ProductStockDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
