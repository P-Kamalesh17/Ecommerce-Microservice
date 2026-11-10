using OrderAPI.Model;
using OrderAPI.Model.DTO;

namespace OrderAPI.Services.IServices
{
    public interface IOrderService
    {
        /// <summary>
        /// Checks stock availability (read-only) and creates a Pending order from the user's cart.
        /// The shipping address falls back to the one on the user's profile. No payment is created here.
        /// </summary>
        Task<OrderDto> Checkout(string userId, CheckoutDto checkoutDto, string authorizationHeader);
        /// <summary>The caller's orders, each with its PaymentId / PaymentStatus (no PaymentId means still Pending).</summary>
        Task<List<OrderDto>> GetMyOrders(string userId, string authorizationHeader);
        Task<List<OrderDto>> GetAllOrders(OrderStatus? orderStatus);

        /// <summary>Returns null if missing. Throws UnauthorizedAccessException if a non-privileged caller asks for another user's order.</summary>
        Task<OrderDto?> GetOrderById(int id, string callerUserId, bool isPrivileged);

        Task<List<OrderItemDto>> GetOrderItems(int orderId, string callerUserId, bool isPrivileged);
        Task<OrderItemDto?> GetOrderItemById(int id, string callerUserId, bool isPrivileged);

        /// <summary>Pending (or Paid) to Processing. Takes the stock from ProductAPI unless the payment already took it.</summary>
        Task<OrderDto> Approve(int id);

        /// <summary>Customer payment confirmed: takes the stock from ProductAPI and moves Pending to Paid. Repeating it is safe and takes the stock only once.</summary>
        Task<OrderDto> MarkPaid(int id, string callerUserId);

        /// <summary>Customer payment failed: Pending to Cancelled and the items go back into the customer's cart.</summary>
        Task<OrderDto> PaymentFailed(int id, string callerUserId, string authorizationHeader);

        Task<OrderDto> UpdateStatus(int id, OrderStatus newStatus, bool isAdmin);
        Task<OrderDto> Cancel(int id, string callerUserId, bool isAdmin);
        Task<bool> Delete(int id);
    }
}
