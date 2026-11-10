using PaymentAPI.Model.DTO;

namespace PaymentAPI.Services.IServices
{
    public interface IPaymentService
    {
        Task<List<PaymentDto>> GetAllPayments();

        /// <summary>Returns null if missing. Throws UnauthorizedAccessException when a customer asks for another user's payment.</summary>
        Task<PaymentDto?> GetPaymentById(int id, string callerUserId, bool isStaff);
        Task<PaymentDto?> GetPaymentByOrderId(int orderId, string callerUserId, bool isStaff);
        Task<List<PaymentDto>> GetPaymentsByUserId(string userId, string callerUserId, bool isStaff);

        /// <summary>
        /// The customer paid. OrderAPI re-checks stock and marks the order Paid first; only then is the Success payment recorded.
        /// Idempotent: a repeat returns the existing Success payment.
        /// </summary>
        Task<PaymentDto> ConfirmPayment(ConfirmPaymentDto confirmPaymentDto, string authorizationHeader);

        /// <summary>
        /// The customer's payment failed. OrderAPI cancels the order and puts its items back in the cart; a Failed payment is recorded.
        /// Idempotent: a repeat returns the existing Failed payment.
        /// </summary>
        Task<PaymentDto> FailPayment(FailPaymentDto failPaymentDto, string authorizationHeader);

        Task<PaymentDto> RefundPayment(int id);
    }
}
