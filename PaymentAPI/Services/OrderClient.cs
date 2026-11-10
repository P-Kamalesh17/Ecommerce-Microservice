using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using PaymentAPI.Model.DTO;
using PaymentAPI.Services.IServices;

namespace PaymentAPI.Services
{
    public class OrderClient : IOrderClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OrderSummaryDto?> GetOrderAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/order/get-order-by-id/{orderId}");
            SetAuthorization(request, authorizationHeader);

            var response = await _httpClientFactory.CreateClient("OrderAPI").SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("You do not have access to this order.");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrderSummaryDto>(cancellationToken);
        }

        public Task MarkOrderPaidAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default)
        {
            return PutOrderActionAsync($"api/order/{orderId}/mark-paid", authorizationHeader, cancellationToken);
        }

        public Task MarkOrderPaymentFailedAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default)
        {
            return PutOrderActionAsync($"api/order/{orderId}/payment-failed", authorizationHeader, cancellationToken);
        }

        private async Task PutOrderActionAsync(string url, string authorizationHeader, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            SetAuthorization(request, authorizationHeader);

            var response = await _httpClientFactory.CreateClient("OrderAPI").SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // OrderAPI answers with { "message": "..." }; pass its wording on (409 = out of stock / wrong state).
            var message = await ReadMessageAsync(response, cancellationToken);

            throw response.StatusCode switch
            {
                HttpStatusCode.Conflict => new InvalidOperationException(message ?? "The order cannot be updated."),
                HttpStatusCode.NotFound => new KeyNotFoundException(message ?? "Order not found."),
                HttpStatusCode.Forbidden => new UnauthorizedAccessException(message ?? "You do not have access to this order."),
                _ => new HttpRequestException($"OrderAPI returned {(int)response.StatusCode}.", null, response.StatusCode)
            };
        }

        private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                return doc.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static void SetAuthorization(HttpRequestMessage request, string authorizationHeader)
        {
            if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization))
            {
                request.Headers.Authorization = authorization;
            }
        }
    }
}
