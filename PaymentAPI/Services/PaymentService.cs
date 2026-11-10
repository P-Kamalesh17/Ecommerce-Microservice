using Microsoft.EntityFrameworkCore;
using PaymentAPI.Data;
using PaymentAPI.Model;
using PaymentAPI.Model.DTO;
using PaymentAPI.Services.IServices;

namespace PaymentAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private const string CancelledOrder = "Cancelled";

        private readonly AppDbContext _context;
        private readonly IOrderClient _orderClient;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(AppDbContext context, IOrderClient orderClient, ILogger<PaymentService> logger)
        {
            _context = context;
            _orderClient = orderClient;
            _logger = logger;
        }

        public async Task<List<PaymentDto>> GetAllPayments()
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(ToDto).ToList();
        }

        public async Task<PaymentDto?> GetPaymentById(int id, string callerUserId, bool isStaff)
        {
            var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return payment == null ? null : ToAuthorizedDto(payment, callerUserId, isStaff);
        }

        public async Task<PaymentDto?> GetPaymentByOrderId(int orderId, string callerUserId, bool isStaff)
        {
            var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId);
            return payment == null ? null : ToAuthorizedDto(payment, callerUserId, isStaff);
        }

        public async Task<List<PaymentDto>> GetPaymentsByUserId(string userId, string callerUserId, bool isStaff)
        {
            if (!isStaff && userId != callerUserId)
            {
                throw new UnauthorizedAccessException("You can only view your own payments.");
            }

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(ToDto).ToList();
        }

        public async Task<PaymentDto> ConfirmPayment(ConfirmPaymentDto confirmPaymentDto, string authorizationHeader)
        {
            // The caller's own token goes to OrderAPI, which rejects orders that are not theirs (403).
            var order = await _orderClient.GetOrderAsync(confirmPaymentDto.OrderId, authorizationHeader)
                ?? throw new KeyNotFoundException("Order not found.");

            var existing = await FindByOrderIdAsync(order.Id);
            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Success)
                {
                    return ToDto(existing);
                }

                throw new InvalidOperationException($"This order already has a {existing.Status} payment.");
            }

            if (order.OrderStatus == CancelledOrder)
            {
                throw new InvalidOperationException("Cannot confirm a payment for a cancelled order.");
            }

            // OrderAPI re-checks stock and moves the order Pending to Paid. If a product is short it answers 409
            // and nothing is recorded here, so the order stays Pending. A retry after a crash is safe (idempotent).
            await _orderClient.MarkOrderPaidAsync(order.Id, authorizationHeader);

            var now = DateTime.UtcNow;

            // Amount and owner come from the order, never from the client.
            var payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Success,
                TransactionId = confirmPaymentDto.TransactionId.Trim(),
                PaidAt = now,
                CreatedAt = now
            };

            return await SaveNewPaymentAsync(payment);
        }

        public async Task<PaymentDto> FailPayment(FailPaymentDto failPaymentDto, string authorizationHeader)
        {
            var order = await _orderClient.GetOrderAsync(failPaymentDto.OrderId, authorizationHeader)
                ?? throw new KeyNotFoundException("Order not found.");

            var existing = await FindByOrderIdAsync(order.Id);
            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Failed)
                {
                    return ToDto(existing);
                }

                throw new InvalidOperationException($"This order already has a {existing.Status} payment, so it cannot be failed.");
            }

            // OrderAPI cancels the Pending order and puts its items back into the customer's cart (idempotent).
            await _orderClient.MarkOrderPaymentFailedAsync(order.Id, authorizationHeader);

            var payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Failed,
                CreatedAt = DateTime.UtcNow
            };

            return await SaveNewPaymentAsync(payment);
        }

        private Task<Payment?> FindByOrderIdAsync(int orderId)
        {
            return _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId);
        }

        private async Task<PaymentDto> SaveNewPaymentAsync(Payment payment)
        {
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent request recorded it first (unique index on OrderId).
                _context.Entry(payment).State = EntityState.Detached;
                var winner = await FindByOrderIdAsync(payment.OrderId);
                if (winner == null)
                {
                    throw;
                }

                _logger.LogInformation("Payment for order {OrderId} was recorded by a concurrent request.", payment.OrderId);
                return ToDto(winner);
            }

            return ToDto(payment);
        }

        public async Task<PaymentDto> RefundPayment(int id)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (payment.Status != PaymentStatus.Success)
            {
                throw new InvalidOperationException("Only a successful payment can be refunded.");
            }

            payment.Status = PaymentStatus.Refunded;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ToDto(payment);
        }

        private static PaymentDto ToAuthorizedDto(Payment payment, string callerUserId, bool isStaff)
        {
            if (!isStaff && payment.UserId != callerUserId)
            {
                throw new UnauthorizedAccessException("You do not have access to this payment.");
            }

            return ToDto(payment);
        }

        private static PaymentDto ToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Status = payment.Status.ToString(),
                TransactionId = payment.TransactionId,
                PaidAt = payment.PaidAt,
                CreatedAt = payment.CreatedAt,
                UpdatedAt = payment.UpdatedAt
            };
        }
    }
}
