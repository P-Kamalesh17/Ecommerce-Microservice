using CartAPI.Model.DTO;

namespace CartAPI.Services.IServices
{
    public interface ICartService
    {
        Task<CartDto?> GetCartByUserId(string userId);
        Task<CartSummaryDto> GetCartSummary(string userId);
        Task AddItem(string userId, AddCartItemDto addCartItemDto);
        Task UpdateItemQuantity(string userId, int cartItemId, UpdateCartItemDto updateCartItemDto);
        Task RemoveItem(string userId, int cartItemId);
        Task ClearCart(string userId);
    }
}
