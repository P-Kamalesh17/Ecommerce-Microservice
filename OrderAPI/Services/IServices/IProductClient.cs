using OrderAPI.Model.DTO;

namespace OrderAPI.Services.IServices
{
    public interface IProductClient
    {
        /// <summary>Open endpoint (no token). Returns null when the product does not exist.</summary>
        Task<ProductStockDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically takes stock for all items, or none. Throws InvalidOperationException (with ProductAPI's message)
        /// on insufficient stock or an unknown product. Authenticated with the internal service key, not a user token.
        /// </summary>
        Task ReserveStockAsync(IEnumerable<StockItemDto> items, CancellationToken cancellationToken = default);

        /// <summary>Gives stock back. Authenticated with the internal service key, not a user token.</summary>
        Task ReleaseStockAsync(IEnumerable<StockItemDto> items, CancellationToken cancellationToken = default);
    }
}
