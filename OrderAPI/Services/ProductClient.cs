using System.Net;
using System.Text.Json;
using ECommerce.Shared.Authorization;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Services
{
    public class ProductClient : IProductClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _internalKey;

        public ProductClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _internalKey = InternalServiceKey.Require(configuration);
        }

        public async Task<ProductStockDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient("ProductAPI");
            var response = await client.GetAsync($"api/product/get-product-by-id/{productId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductStockDto>(cancellationToken);
        }

        public async Task ReserveStockAsync(IEnumerable<StockItemDto> items, CancellationToken cancellationToken = default)
        {
            var response = await PostStockAsync("api/product/reserve-stock", items, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
            {
                // ProductAPI explains which product is short or missing.
                throw new InvalidOperationException(
                    await ReadMessageAsync(response, cancellationToken) ?? "One or more items are out of stock.");
            }

            response.EnsureSuccessStatusCode();
        }

        public async Task ReleaseStockAsync(IEnumerable<StockItemDto> items, CancellationToken cancellationToken = default)
        {
            var response = await PostStockAsync("api/product/release-stock", items, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task<HttpResponseMessage> PostStockAsync(string url, IEnumerable<StockItemDto> items, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(items)
            };

            // Service-to-service call: ProductAPI accepts this key in place of a staff token.
            request.Headers.Add(InternalServiceKey.HeaderName, _internalKey);

            return await _httpClientFactory.CreateClient("ProductAPI").SendAsync(request, cancellationToken);
        }

        private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                return document.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
