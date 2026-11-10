using PaymentAPI.Model.DTO;

namespace PaymentAPI.Services.IServices
{
    public interface IOrderClient
    {
        /// <summary>
        /// Reads an order with the caller's own token, so OrderAPI enforces ownership.
        /// Returns null when missing; throws UnauthorizedAccessException when the caller may not see it.
        /// </summary>
        Task<OrderSummaryDto?> GetOrderAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asks OrderAPI to re-check stock and move the order Pending to Paid (idempotent).
        /// Throws InvalidOperationException (with OrderAPI's message) when a product is short or the order cannot be paid.
        /// </summary>
        Task MarkOrderPaidAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asks OrderAPI to cancel the Pending order and put its items back into the customer's cart (idempotent).
        /// </summary>
        Task MarkOrderPaymentFailedAsync(int orderId, string authorizationHeader, CancellationToken cancellationToken = default);
    }
}
