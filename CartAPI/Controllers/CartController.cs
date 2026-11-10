using CartAPI.Model.DTO;
using CartAPI.Services.IServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CartAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var cart = await _cartService.GetCartByUserId(CurrentUserId());

                if (cart == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Cart not found for this user." });
                }

                return StatusCode(StatusCodes.Status200OK, cart);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the cart");
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetCartSummary()
        {
            try
            {
                var summary = await _cartService.GetCartSummary(CurrentUserId());
                return StatusCode(StatusCodes.Status200OK, summary);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the cart summary");
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemDto addCartItemDto)
        {
            try
            {
                await _cartService.AddItem(CurrentUserId(), addCartItemDto);
                return StatusCode(StatusCodes.Status200OK, new { message = "Item added to cart." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "adding the item to the cart");
            }
        }

        [HttpPut("items/{cartItemId:int}")]
        public async Task<IActionResult> UpdateItemQuantity(int cartItemId, [FromBody] UpdateCartItemDto updateCartItemDto)
        {
            try
            {
                await _cartService.UpdateItemQuantity(CurrentUserId(), cartItemId, updateCartItemDto);
                return StatusCode(StatusCodes.Status200OK, new { message = "Cart item updated." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "updating the cart item");
            }
        }

        [HttpDelete("items/{cartItemId:int}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                await _cartService.RemoveItem(CurrentUserId(), cartItemId);
                return StatusCode(StatusCodes.Status200OK, new { message = "Item removed from cart." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "removing the cart item");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                await _cartService.ClearCart(CurrentUserId());
                return StatusCode(StatusCodes.Status200OK, new { message = "Cart cleared." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "clearing the cart");
            }
        }

        // The default inbound claim mapping can rename "sub" to NameIdentifier, so accept both.
        private string CurrentUserId()
        {
            return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("The token does not contain a user id.");
        }

        private IActionResult ErrorResult(Exception ex, string operation)
        {
            return ex switch
            {
                ArgumentException => StatusCode(StatusCodes.Status400BadRequest, new { message = ex.Message }),
                KeyNotFoundException => StatusCode(StatusCodes.Status404NotFound, new { message = ex.Message }),
                UnauthorizedAccessException => StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message }),
                HttpRequestException => StatusCode(StatusCodes.Status500InternalServerError, new { message = "The product service is unavailable. Please try again later." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while {operation}." })
            };
        }
    }
}
