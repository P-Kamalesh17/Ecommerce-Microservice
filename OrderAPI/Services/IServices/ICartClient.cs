using OrderAPI.Model.DTO;

namespace OrderAPI.Services.IServices
{
    public interface ICartClient
    {
        Task<CartDto?> GetCartAsync(string authorizationHeader, CancellationToken cancellationToken = default);
        /// <summary>Adds a product back to the caller's cart (used when a payment fails).</summary>
        Task AddItemAsync(string authorizationHeader, int productId, int quantity, CancellationToken cancellationToken = default);
        Task ClearCartAsync(string authorizationHeader, CancellationToken cancellationToken = default);
    }
}
