using OrderAPI.Model.DTO;

namespace OrderAPI.Services.IServices
{
    public interface IPaymentClient
    {
        /// <summary>
        /// Reads the caller's own payments from PaymentAPI. An order with no payment here is still Pending.
        /// <paramref name="authorizationHeader"/> is the customer's own Authorization header value ("Bearer ...").
        /// </summary>
        Task<List<PaymentSummaryDto>> GetMyPaymentsAsync(string authorizationHeader, CancellationToken cancellationToken = default);
    }
}
