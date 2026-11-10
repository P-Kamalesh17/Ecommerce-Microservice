using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ECommerce.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentAPI.Model.DTO;
using PaymentAPI.Services.IServices;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet("get-all-payments")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetAllPayments()
        {
            try
            {
                _logger.LogInformation("Fetching all payments.");
                var payments = await _paymentService.GetAllPayments();
                return StatusCode(StatusCodes.Status200OK, payments);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving payments");
            }
        }

        [HttpGet("get-payment/{id:int}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentById(id, CurrentUserId(), IsStaff());

                if (payment == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Payment not found." });
                }

                return StatusCode(StatusCodes.Status200OK, payment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the payment");
            }
        }

        [HttpGet("get-payment-by-order/{orderId:int}")]
        public async Task<IActionResult> GetPaymentByOrder(int orderId)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByOrderId(orderId, CurrentUserId(), IsStaff());

                if (payment == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "No payment found for this order." });
                }

                return StatusCode(StatusCodes.Status200OK, payment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the payment for the order");
            }
        }

        // The user id comes from the caller's token, so the frontend never sends it.
        [HttpGet("get-my-payments")]
        public async Task<IActionResult> GetMyPayments()
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByUserId(CurrentUserId(), CurrentUserId(), IsStaff());
                return StatusCode(StatusCodes.Status200OK, payments);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving your payments");
            }
        }

        // Admin lookup of another user's payments.
        [HttpGet("get-user-payments/{userId}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetUserPayments(string userId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByUserId(userId, CurrentUserId(), IsStaff());
                return StatusCode(StatusCodes.Status200OK, payments);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the user's payments");
            }
        }

        // The customer reports the result of their own payment; the order id is the key (no payment row exists before this).
        [HttpPost("confirm-payment")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentDto confirmPaymentDto)
        {
            try
            {
                var payment = await _paymentService.ConfirmPayment(confirmPaymentDto, AuthorizationHeader());
                return StatusCode(StatusCodes.Status200OK, payment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "confirming the payment");
            }
        }

        [HttpPost("fail-payment")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> FailPayment([FromBody] FailPaymentDto failPaymentDto)
        {
            try
            {
                var payment = await _paymentService.FailPayment(failPaymentDto, AuthorizationHeader());
                return StatusCode(StatusCodes.Status200OK, payment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "marking the payment as failed");
            }
        }

        [HttpPost("refund-payment/{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> RefundPayment(int id)
        {
            try
            {
                var payment = await _paymentService.RefundPayment(id);
                return StatusCode(StatusCodes.Status200OK, payment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "refunding the payment");
            }
        }

        // The default inbound claim mapping can rename "sub" to NameIdentifier, so accept both.
        private string CurrentUserId()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("The token does not contain a user id.");
        }

        private bool IsStaff()
        {
            return User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.Shipment);
        }

        // Full "Bearer ..." value, passed explicitly to OrderAPI so it enforces the caller's own permissions.
        private string AuthorizationHeader()
        {
            return Request.Headers.Authorization.ToString();
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
                HttpRequestException => StatusCode(StatusCodes.Status502BadGateway, new { message = "The order service is unavailable or rejected the request. Please try again later." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while {operation}." })
            };
        }
    }
}
