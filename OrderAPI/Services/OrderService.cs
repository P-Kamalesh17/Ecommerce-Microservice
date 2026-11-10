using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.Model;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Services
{
    public class OrderService : IOrderService
    {
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        // Statuses a Shipment user (non-admin) may set through PUT {id}/status.
        private static readonly OrderStatus[] ShipmentStatuses = { OrderStatus.Shipped, OrderStatus.Delivered };

        // Stock is taken when the payment is confirmed (Paid) or the order is approved (Processing), and stays taken
        // until the order is cancelled or deleted. Pending holds none; Delivered is sold.
        private static readonly OrderStatus[] StockHeldStatuses = { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Shipped };

        private readonly AppDbContext _context;
        private readonly ICartClient _cartClient;
        private readonly IProductClient _productClient;
        private readonly IPaymentClient _paymentClient;
        private readonly IUserClient _userClient;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            AppDbContext context,
            ICartClient cartClient,
            IProductClient productClient,
            IPaymentClient paymentClient,
            IUserClient userClient,
            ILogger<OrderService> logger)
        {
            _context = context;
            _cartClient = cartClient;
            _productClient = productClient;
            _paymentClient = paymentClient;
            _userClient = userClient;
            _logger = logger;
        }

        public async Task<OrderDto> Checkout(string userId, CheckoutDto checkoutDto, string authorizationHeader)
        {
            var cart = await _cartClient.GetCartAsync(authorizationHeader);
            if (cart == null || cart.Items.Count == 0)
            {
                throw new ArgumentException("Your cart is empty.");
            }

            await EnsureStockAvailableAsync(cart.Items.Select(i => (i.ProductId, i.ProductName, i.Quantity)));

            // No address in the request: use the one saved on the user's profile.
            var shippingAddress = checkoutDto.ShippingAddress?.Trim();
            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                var profile = await _userClient.GetProfileAsync(authorizationHeader);
                shippingAddress = profile?.Address?.Trim();
            }

            if (string.IsNullOrWhiteSpace(shippingAddress))
            {
                throw new ArgumentException("No shipping address was given and none is saved on your profile.");
            }

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.UnitPrice * i.Quantity
                }).ToList()
            };
            order.TotalAmount = order.OrderItems.Sum(i => i.Subtotal);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            try
            {
                await _cartClient.ClearCartAsync(authorizationHeader);
            }
            catch (Exception ex)
            {
                // The order is already saved; do not fail checkout over a stale cart.
                _logger.LogWarning(ex, "Order {OrderId} was created but the cart for user {UserId} could not be cleared.", order.Id, userId);
            }

            return ToDto(order);
        }

        public async Task<List<OrderDto>> GetMyOrders(string userId, string authorizationHeader)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ThenByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = orders.Select(ToDto).ToList();

            try
            {
                var payments = await _paymentClient.GetMyPaymentsAsync(authorizationHeader);
                var byOrder = payments.GroupBy(p => p.OrderId).ToDictionary(g => g.Key, g => g.First());

                foreach (var dto in dtos)
                {
                    if (byOrder.TryGetValue(dto.Id, out var payment))
                    {
                        dto.PaymentId = payment.Id;
                        dto.PaymentStatus = payment.Status;
                    }
                    else
                    {
                        dto.PaymentStatus = "Pending";
                    }
                }
            }
            catch (Exception ex)
            {
                // The orders are still useful without payment info; PaymentId/PaymentStatus stay null.
                _logger.LogWarning(ex, "Could not load payments for user {UserId}.", userId);
            }

            return dtos;
        }

        public async Task<List<OrderDto>> GetAllOrders(OrderStatus? orderStatus)
        {
            IQueryable<Order> query = _context.Orders.AsNoTracking().Include(o => o.OrderItems);

            if (orderStatus.HasValue)
            {
                query = query.Where(o => o.Status == orderStatus.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.UpdatedAt)
                .ThenByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(ToDto).ToList();
        }

        public async Task<OrderDto?> GetOrderById(int id, string callerUserId, bool isPrivileged)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return null;
            }

            EnsureCanAccess(order, callerUserId, isPrivileged);
            return ToDto(order);
        }

        public async Task<List<OrderItemDto>> GetOrderItems(int orderId, string callerUserId, bool isPrivileged)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            EnsureCanAccess(order, callerUserId, isPrivileged);
            return order.OrderItems.Select(ToDto).ToList();
        }

        public async Task<OrderItemDto?> GetOrderItemById(int id, string callerUserId, bool isPrivileged)
        {
            var item = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (item == null)
            {
                return null;
            }

            EnsureCanAccess(item.Order, callerUserId, isPrivileged);
            return ToDto(item);
        }

        public async Task<OrderDto> Approve(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Paid)
            {
                throw new ArgumentException($"Only Pending or Paid orders can be approved. This order is {order.Status}.");
            }

            var stockItems = ToStockItems(order.OrderItems);

            // A confirmed payment already took the stock; only a Pending order still needs it.
            var takenNow = order.Status == OrderStatus.Pending;

            if (takenNow)
            {
                // Throws InvalidOperationException when any item is short (mapped to 409); the order stays as it is.
                await _productClient.ReserveStockAsync(stockItems);
            }

            try
            {
                order.Status = OrderStatus.Processing;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (takenNow)
                {
                    await TryReleaseStockAsync(stockItems, $"order {id} approval could not be saved");
                }

                throw;
            }

            return ToDto(order);
        }

        public async Task<OrderDto> UpdateStatus(int id, OrderStatus newStatus, bool isAdmin)
        {
            if (newStatus == OrderStatus.Processing)
            {
                throw new ArgumentException("Use the approve endpoint to move an order to Processing: it reserves stock.");
            }

            if (newStatus == OrderStatus.Cancelled)
            {
                throw new ArgumentException("Use the cancel endpoint to cancel an order: it returns stock.");
            }

            if (!isAdmin && !ShipmentStatuses.Contains(newStatus))
            {
                throw new UnauthorizedAccessException("Shipment users can only set the status to Shipped or Delivered.");
            }

            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            // Repeating a payment confirmation must be safe: Paid on an already Paid order changes nothing.
            if (order.Status == newStatus && newStatus == OrderStatus.Paid)
            {
                return ToDto(order);
            }

            EnsureTransitionAllowed(order.Status, newStatus);

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ToDto(order);
        }

        public async Task<OrderDto> Cancel(int id, string callerUserId, bool isAdmin)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            if (!isAdmin)
            {
                if (order.UserId != callerUserId)
                {
                    throw new UnauthorizedAccessException("You can only cancel your own orders.");
                }

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Paid)
                {
                    throw new ArgumentException($"An order that is {order.Status} can no longer be cancelled.");
                }
            }

            EnsureTransitionAllowed(order.Status, OrderStatus.Cancelled);

            // A Paid or Processing order holds stock; give it back. A Pending order holds none.
            var hadStock = order.Status == OrderStatus.Paid || order.Status == OrderStatus.Processing;

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (hadStock)
            {
                await TryReleaseStockAsync(ToStockItems(order.OrderItems), $"order {order.Id} cancelled");
            }

            return ToDto(order);
        }

        public async Task<bool> Delete(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return false;
            }

            var hadStock = StockHeldStatuses.Contains(order.Status);
            var stockItems = ToStockItems(order.OrderItems);

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            if (hadStock)
            {
                await TryReleaseStockAsync(stockItems, $"order {id} deleted");
            }

            return true;
        }

        public async Task<OrderDto> MarkPaid(int id, string callerUserId)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            EnsureOwner(order, callerUserId);

            // Repeating a confirmation is safe: the stock was already taken the first time.
            if (order.Status == OrderStatus.Paid)
            {
                return ToDto(order);
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException($"Only a Pending order can be paid. This order is {order.Status}.");
            }

            // Friendly check first, so the customer is told which product is short.
            await EnsureStockAvailableAsync(order.OrderItems.Select(i => (i.ProductId, i.ProductName, i.Quantity)));

            var previousUpdatedAt = order.UpdatedAt;

            // Claim the order: of two simultaneous confirmations only one can move it from Pending to Paid,
            // so the stock is taken exactly once.
            var claimed = await _context.Orders
                .Where(o => o.Id == id && o.Status == OrderStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OrderStatus.Paid)
                    .SetProperty(o => o.UpdatedAt, (DateTime?)DateTime.UtcNow));

            if (claimed == 0)
            {
                await _context.Entry(order).ReloadAsync();

                if (order.Status == OrderStatus.Paid)
                {
                    return ToDto(order);
                }

                throw new InvalidOperationException($"Only a Pending order can be paid. This order is {order.Status}.");
            }

            try
            {
                // Throws InvalidOperationException (409) when a product is short; nothing is taken in that case.
                await _productClient.ReserveStockAsync(ToStockItems(order.OrderItems));
            }
            catch
            {
                // Put the order back to Pending so the customer can retry or report the payment as failed.
                try
                {
                    await _context.Orders
                        .Where(o => o.Id == id && o.Status == OrderStatus.Paid)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(o => o.Status, OrderStatus.Pending)
                            .SetProperty(o => o.UpdatedAt, previousUpdatedAt));
                }
                catch (Exception revertEx)
                {
                    _logger.LogError(revertEx,
                        "Order {OrderId} was marked Paid but its stock could not be taken, and it could not be put back to Pending.", id);
                }

                throw;
            }

            await _context.Entry(order).ReloadAsync();
            return ToDto(order);
        }

        public async Task<OrderDto> PaymentFailed(int id, string callerUserId, string authorizationHeader)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            EnsureOwner(order, callerUserId);

            // Repeating a failure report is safe and does not add the items to the cart twice.
            if (order.Status == OrderStatus.Cancelled)
            {
                return ToDto(order);
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException($"Only a Pending order can have its payment failed. This order is {order.Status}.");
            }

            // Stock is only taken at approval, so a Pending order has nothing to give back.
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            foreach (var item in order.OrderItems)
            {
                try
                {
                    await _cartClient.AddItemAsync(authorizationHeader, item.ProductId, item.Quantity);
                }
                catch (Exception ex)
                {
                    // Best effort: the product may be gone, out of stock, or over the cart limit.
                    _logger.LogWarning(ex, "Could not put product {ProductId} x{Quantity} from cancelled order {OrderId} back into the cart.",
                        item.ProductId, item.Quantity, order.Id);
                }
            }

            return ToDto(order);
        }

        private static void EnsureOwner(Order order, string callerUserId)
        {
            if (order.UserId != callerUserId)
            {
                throw new UnauthorizedAccessException("You do not have access to this order.");
            }
        }

        // Read-only check: the real decrement happens at approval.
        private async Task EnsureStockAvailableAsync(IEnumerable<(int ProductId, string ProductName, int Quantity)> cartItems)
        {
            var shortages = new List<string>();

            foreach (var line in cartItems.GroupBy(i => i.ProductId))
            {
                var wanted = line.Sum(i => i.Quantity);
                var product = await _productClient.GetProductAsync(line.Key);

                if (product == null)
                {
                    shortages.Add($"{line.First().ProductName} is no longer available");
                }
                else if (product.StockQuantity < wanted)
                {
                    shortages.Add($"{product.Name}: requested {wanted}, only {product.StockQuantity} in stock");
                }
            }

            if (shortages.Count > 0)
            {
                throw new InvalidOperationException("Not enough stock. " + string.Join("; ", shortages) + ".");
            }
        }

        private static void EnsureCanAccess(Order order, string callerUserId, bool isPrivileged)
        {
            if (!isPrivileged && order.UserId != callerUserId)
            {
                throw new UnauthorizedAccessException("You do not have access to this order.");
            }
        }

        private static void EnsureTransitionAllowed(OrderStatus from, OrderStatus to)
        {
            if (!AllowedTransitions[from].Contains(to))
            {
                throw new ArgumentException($"An order cannot move from {from} to {to}.");
            }
        }

        private async Task TryReleaseStockAsync(List<StockItemDto> items, string reason)
        {
            try
            {
                await _productClient.ReleaseStockAsync(items);
            }
            catch (Exception ex)
            {
                // Needs manual reconciliation: log enough to find it.
                _logger.LogError(ex, "Failed to release stock ({Reason}). Items: {Items}", reason,
                    string.Join(", ", items.Select(i => $"{i.ProductId}x{i.Quantity}")));
            }
        }

        private static List<StockItemDto> ToStockItems(IEnumerable<OrderItem> items)
        {
            return items
                .GroupBy(i => i.ProductId)
                .Select(g => new StockItemDto { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();
        }

        private static OrderDto ToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderStatus = order.Status.ToString(),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(ToDto).ToList()
            };
        }

        private static OrderItemDto ToDto(OrderItem item)
        {
            return new OrderItemDto
            {
                Id = item.Id,
                OrderId = item.OrderId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal
            };
        }
    }
}
