using System.Net.Http.Json;
using CartAPI.Model.DTO;
using CartAPI.Services.IServices;

namespace CartAPI.Services
{
    public class ProductClient : IProductClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<ProductDto>> GetAllProducts(CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient("ProductAPI");

            var products = await client.GetFromJsonAsync<List<ProductDto>>("api/product/get-all-products", cancellationToken);

            return products ?? new List<ProductDto>();
        }
    }
}
