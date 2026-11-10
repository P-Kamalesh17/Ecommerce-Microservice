using System.Net.Http.Headers;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Services
{
    public class PaymentClient : IPaymentClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<PaymentSummaryDto>> GetMyPaymentsAsync(string authorizationHeader, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/payment/get-my-payments");

            if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization))
            {
                request.Headers.Authorization = authorization;
            }

            var response = await _httpClientFactory.CreateClient("PaymentAPI").SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<PaymentSummaryDto>>(cancellationToken) ?? new List<PaymentSummaryDto>();
        }
    }
}
