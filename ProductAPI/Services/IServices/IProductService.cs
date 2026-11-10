using ProductAPI.Model.DTO;

namespace ProductAPI.Services.IServices
{
    public interface IProductService
    {
        Task<ProductDto> CreateProduct(CreateProductDto createProductDto);
        Task<bool> DeleteProductById(int id);
        Task<List<ProductDto>> GetAllProducts();
        Task<List<ProductDto>> GetProductsByCategoryId(int categoryId);
        Task<ProductDto?> GetProductById(int id);
        Task<ProductDto?> UpdateProductById(int id, CreateProductDto createProductDto);
        Task<List<ProductDto>> FilterProducts(decimal? minPrice, decimal? maxPrice, int? categoryId);

        /// <summary>All-or-nothing stock decrement. Throws InvalidOperationException when any product is short.</summary>
        Task ReserveStock(IEnumerable<StockItemDto> items);
        Task ReleaseStock(IEnumerable<StockItemDto> items);
    }
}
