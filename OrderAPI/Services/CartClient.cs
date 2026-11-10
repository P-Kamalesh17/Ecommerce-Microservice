using System.Net;
using System.Net.Http.Headers;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Services
{
    public class CartClient : ICartClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CartClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // CartAPI reads the user id from the caller's own token, so the Authorization header is forwarded as-is.
        public async Task<CartDto?> GetCartAsync(string authorizationHeader, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/cart");
            SetAuthorization(request, authorizationHeader);

            var response = await _httpClientFactory.CreateClient("CartAPI").SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken);
        }

        public async Task AddItemAsync(string authorizationHeader, int productId, int quantity, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/cart/items")
            {
                Content = JsonContent.Create(new { ProductId = productId, Quantity = quantity })
            };
            SetAuthorization(request, authorizationHeader);

            var response = await _httpClientFactory.CreateClient("CartAPI").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task ClearCartAsync(string authorizationHeader, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, "api/cart");
            SetAuthorization(request, authorizationHeader);

            var response = await _httpClientFactory.CreateClient("CartAPI").SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            response.EnsureSuccessStatusCode();
        }

        private static void SetAuthorization(HttpRequestMessage request, string authorizationHeader)
        {
            if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var header))
            {
                request.Headers.Authorization = header;
            }
        }
    }
}
