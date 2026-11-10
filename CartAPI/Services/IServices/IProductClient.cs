using CartAPI.Model.DTO;

namespace CartAPI.Services.IServices
{
    public interface IProductClient
    {
        Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default);
    }
}
