using DAL.Models;
using CartAPI.Data;
using CartAPI.Model.DTO;
using CartAPI.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace CartAPI.Services
{
    public class CartService : ICartService
    {
        private const int MaxQuantityPerItem = 100;

        private readonly AppDbContext _context;
        private readonly IProductClient _productClient;

        public CartService(AppDbContext context, IProductClient productClient)
        {
            _context = context;
            _productClient = productClient;
        }

        public async Task<CartDto?> GetCartByUserId(string userId)
        {
            var cart = await GetCartWithItems(userId);

            if (cart == null)
            {
                return null;
            }

            var products = await GetProductLookup();

            return MapToCartDto(cart, products);
        }

        public async Task<CartSummaryDto> GetCartSummary(string userId)
        {
            var cart = await GetCartWithItems(userId);

            if (cart == null)
            {
                throw new KeyNotFoundException("Cart not found for this user.");
            }

            var products = await GetProductLookup();

            return ComputeSummary(cart, products);
        }

        public async Task AddItem(string userId, AddCartItemDto addCartItemDto)
        {
            if (addCartItemDto == null)
            {
                throw new ArgumentNullException(
                    nameof(addCartItemDto),
                    "Cart item details are required."
                );
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.");
            }

            EnsureValidQuantity(addCartItemDto.Quantity);

            var products = await GetProductLookup();

            if (!products.TryGetValue(addCartItemDto.ProductId, out var product))
            {
                throw new KeyNotFoundException("Invalid product ID.");
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.CartItems
                .FirstOrDefault(ci => ci.ProductId == addCartItemDto.ProductId);

            var newQuantity = (existingItem?.Quantity ?? 0) + addCartItemDto.Quantity;

            EnsureValidQuantity(newQuantity);
            EnsureInStock(product, newQuantity);

            if (existingItem != null)
            {
                existingItem.Quantity = newQuantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = addCartItemDto.ProductId,
                    Quantity = addCartItemDto.Quantity
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemQuantity(string userId, int cartItemId, UpdateCartItemDto updateCartItemDto)
        {
            if (updateCartItemDto == null)
            {
                throw new ArgumentNullException(
                    nameof(updateCartItemDto),
                    "Cart item details are required."
                );
            }

            EnsureValidQuantity(updateCartItemDto.Quantity);

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                throw new KeyNotFoundException("Invalid cart item ID.");
            }

            var products = await GetProductLookup();

            if (!products.TryGetValue(cartItem.ProductId, out var product))
            {
                throw new KeyNotFoundException("This product is no longer available.");
            }

            EnsureInStock(product, updateCartItemDto.Quantity);

            cartItem.Quantity = updateCartItemDto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task RemoveItem(string userId, int cartItemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                throw new KeyNotFoundException("Invalid cart item ID.");
            }

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ClearCart(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                throw new KeyNotFoundException("Cart not found for this user.");
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static void EnsureValidQuantity(int quantity)
        {
            if (quantity < 1 || quantity > MaxQuantityPerItem)
            {
                throw new ArgumentException($"Quantity must be between 1 and {MaxQuantityPerItem}.");
            }
        }

        private static void EnsureInStock(ProductDto product, int quantity)
        {
            if (quantity > product.StockQuantity)
            {
                throw new ArgumentException($"Only {product.StockQuantity} unit(s) of '{product.Name}' are in stock.");
            }
        }

        private Task<Cart?> GetCartWithItems(string userId)
        {
            return _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        private async Task<Dictionary<int, ProductDto>> GetProductLookup()
        {
            var products = await _productClient.GetAllProducts();

            return products.ToDictionary(p => p.Id);
        }

        private static CartSummaryDto ComputeSummary(Cart cart, Dictionary<int, ProductDto> products)
        {
            var subtotal = cart.CartItems
                .Sum(ci => products.TryGetValue(ci.ProductId, out var product) ? product.Price * ci.Quantity : 0m);

            var discountAmount = 0m;
            var tax = 0m;
            var shippingCost = 0m;

            return new CartSummaryDto
            {
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                Tax = tax,
                ShippingCost = shippingCost,
                TotalAmount = subtotal - discountAmount + tax + shippingCost
            };
        }

        private static CartDto MapToCartDto(Cart cart, Dictionary<int, ProductDto> products)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Items = cart.CartItems.Select(ci =>
                {
                    products.TryGetValue(ci.ProductId, out var product);
                    var unitPrice = product?.Price ?? 0m;

                    return new CartItemDto
                    {
                        Id = ci.Id,
                        ProductId = ci.ProductId,
                        ProductName = product?.Name ?? "Unavailable product",
                        ImageUrl = product?.ImageUrl ?? string.Empty,
                        UnitPrice = unitPrice,
                        Quantity = ci.Quantity,
                        Subtotal = unitPrice * ci.Quantity
                    };
                }).ToList()
            };
        }
    }
}
