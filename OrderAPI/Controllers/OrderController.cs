using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderAPI.Model;
using OrderAPI.Model.DTO;
using OrderAPI.Services.IServices;

namespace OrderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost("checkout")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto? checkoutDto)
        {
            try
            {
                var order = await _orderService.Checkout(CurrentUserId(), checkoutDto ?? new CheckoutDto(), AuthorizationHeader());
                return StatusCode(StatusCodes.Status201Created, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "placing the order");
            }
        }

        [HttpGet("my-orders")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var orders = await _orderService.GetMyOrders(CurrentUserId(), AuthorizationHeader());
                return StatusCode(StatusCodes.Status200OK, orders);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving your orders");
            }
        }

        [HttpGet("get-all-orders")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Shipment)]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderStatus? orderStatus)
        {
            try
            {
                _logger.LogInformation("Fetching all orders with status filter {OrderStatus}.", orderStatus);
                var orders = await _orderService.GetAllOrders(orderStatus);
                return StatusCode(StatusCodes.Status200OK, orders);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving orders");
            }
        }

        [HttpGet("get-order-by-id/{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderById(id, CurrentUserId(), IsPrivileged());

                if (order == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Order not found." });
                }

                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the order");
            }
        }

        [HttpGet("{orderId:int}/items")]
        public async Task<IActionResult> GetOrderItems(int orderId)
        {
            try
            {
                var items = await _orderService.GetOrderItems(orderId, CurrentUserId(), IsPrivileged());
                return StatusCode(StatusCodes.Status200OK, items);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the order items");
            }
        }

        [HttpGet("items/{id:int}")]
        public async Task<IActionResult> GetOrderItemById(int id)
        {
            try
            {
                var item = await _orderService.GetOrderItemById(id, CurrentUserId(), IsPrivileged());

                if (item == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Order item not found." });
                }

                return StatusCode(StatusCodes.Status200OK, item);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the order item");
            }
        }

        [HttpPut("{id:int}/approve")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Shipment)]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var order = await _orderService.Approve(id);
                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "approving the order");
            }
        }

        [HttpPut("{id:int}/status")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Shipment)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto updateOrderStatusDto)
        {
            try
            {
                var order = await _orderService.UpdateStatus(id, updateOrderStatusDto.Status, User.IsInRole(UserRoles.Admin));
                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "updating the order status");
            }
        }

        // Called by PaymentAPI with the customer's own token when they confirm a payment.
        [HttpPut("{id:int}/mark-paid")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> MarkPaid(int id)
        {
            try
            {
                var order = await _orderService.MarkPaid(id, CurrentUserId());
                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "marking the order as paid");
            }
        }

        // Called by PaymentAPI with the customer's own token when their payment fails.
        [HttpPut("{id:int}/payment-failed")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> PaymentFailed(int id)
        {
            try
            {
                var order = await _orderService.PaymentFailed(id, CurrentUserId(), AuthorizationHeader());
                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "handling the failed payment");
            }
        }

        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Customer)]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var order = await _orderService.Cancel(id, CurrentUserId(), User.IsInRole(UserRoles.Admin));
                return StatusCode(StatusCodes.Status200OK, order);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "cancelling the order");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _orderService.Delete(id);

                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Order not found." });
                }

                return StatusCode(StatusCodes.Status200OK, new { message = "Order deleted successfully." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "deleting the order");
            }
        }

        // The default inbound claim mapping can rename "sub" to NameIdentifier, so accept both.
        private string CurrentUserId()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("The token does not contain a user id.");
        }

        // Full "Bearer ..." value of the caller's request, passed explicitly to staff-only ProductAPI stock calls.
        private string AuthorizationHeader()
        {
            return Request.Headers.Authorization.ToString();
        }

        private bool IsPrivileged()
        {
            return User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.Shipment);
        }

        private IActionResult ErrorResult(Exception ex, string operation)
        {
            if (ex is not (ArgumentException or KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException))
            {
                _logger.LogError(ex, "Unexpected error while {Operation}.", operation);
            }

            return ex switch
            {
                ArgumentException => StatusCode(StatusCodes.Status400BadRequest, new { message = ex.Message }),
                KeyNotFoundException => StatusCode(StatusCodes.Status404NotFound, new { message = ex.Message }),
                UnauthorizedAccessException => StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }),
                InvalidOperationException => StatusCode(StatusCodes.Status409Conflict, new { message = ex.Message }),
                HttpRequestException => StatusCode(StatusCodes.Status502BadGateway, new { message = "A dependent service (cart, product, payment or auth) is unavailable. Please try again later." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while {operation}." })
            };
        }
    }
}
