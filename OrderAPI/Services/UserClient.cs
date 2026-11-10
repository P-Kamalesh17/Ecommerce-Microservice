using System.Net;
using System.Net.Http.Headers;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Services
{
    public class UserClient : IUserClient
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UserClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<UserProfileDto?> GetProfileAsync(string authorizationHeader, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/authentication/me");

            if (AuthenticationHeaderValue.TryParse(authorizationHeader, out var authorization))
            {
                request.Headers.Authorization = authorization;
            }

            var response = await _httpClientFactory.CreateClient("AuthAPI").SendAsync(request, cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken);
        }
    }
}
