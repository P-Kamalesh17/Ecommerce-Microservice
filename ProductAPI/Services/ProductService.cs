using DAL.Models;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Model.DTO;
using ProductAPI.Services.IServices;

namespace ProductAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IImageStorageService _imageStorageService;

        public ProductService(AppDbContext context, IImageStorageService imageStorageService)
        {
            _context = context;
            _imageStorageService = imageStorageService;
        }

        public async Task<ProductDto> CreateProduct(CreateProductDto createProductDto)
        {
            await EnsureCategoryExistsAsync(createProductDto.CategoryId);

            var imageUrl = createProductDto.ImageFile != null
                ? await _imageStorageService.SaveImageAsync(createProductDto.ImageFile)
                : string.Empty;

            var product = new Product
            {
                Name = createProductDto.Name,
                CategoryId = createProductDto.CategoryId,
                Description = createProductDto.Description ?? string.Empty,
                ImageUrl = imageUrl,
                Price = createProductDto.Price,
                StockQuantity = createProductDto.StockQuantity,
                CreatedAt = DateTime.UtcNow
            };
      
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return (await GetProductById(product.Id))!;
        }

        public async Task<bool> DeleteProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return false;
            }

            var imageUrl = product.ImageUrl;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _imageStorageService.DeleteImage(imageUrl);
            return true;
        }
   
        public Task<List<ProductDto>> GetAllProducts()
        {
            return ToProductDtos(_context.Products.AsNoTracking()).ToListAsync();
        }

        public Task<List<ProductDto>> GetProductsByCategoryId(int categoryId)
        {
            return FilterProductsInternal(null, null, categoryId);
        }

        public Task<ProductDto?> GetProductById(int id)
        {
            return ToProductDtos(_context.Products.AsNoTracking().Where(p => p.Id == id))
                .FirstOrDefaultAsync();
        }

        public async Task<ProductDto?> UpdateProductById(int id, CreateProductDto createProductDto)
        {
            await EnsureCategoryExistsAsync(createProductDto.CategoryId);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return null;
            }

            var newImageUrl = createProductDto.ImageFile != null
                ? await _imageStorageService.SaveImageAsync(createProductDto.ImageFile)
                : null;

            var oldImageUrl = product.ImageUrl;

            product.Name = createProductDto.Name;
            product.CategoryId = createProductDto.CategoryId;
            product.Description = createProductDto.Description ?? string.Empty;
            product.Price = createProductDto.Price;
            product.StockQuantity = createProductDto.StockQuantity;
            product.UpdatedAt = DateTime.UtcNow;

            if (newImageUrl != null)
            {
                product.ImageUrl = newImageUrl;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                if (newImageUrl != null)
                {
                    _imageStorageService.DeleteImage(newImageUrl);
                }

                throw;
            }

            if (newImageUrl != null)
            {
                _imageStorageService.DeleteImage(oldImageUrl);
            }

            return await GetProductById(id);
        }

        public Task<List<ProductDto>> FilterProducts(decimal? minPrice, decimal? maxPrice, int? categoryId)
        {
            if (minPrice > maxPrice)
            {
                throw new ArgumentException("minPrice cannot be greater than maxPrice.");
            }

            return FilterProductsInternal(minPrice, maxPrice, categoryId);
        }

        public async Task ReserveStock(IEnumerable<StockItemDto> items)
        {
            var lines = MergeStockItems(items);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var line in lines)
            {
                // Single guarded UPDATE keeps the check-and-decrement atomic under concurrency.
                var updated = await _context.Products
                    .Where(p => p.Id == line.ProductId && p.StockQuantity >= line.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - line.Quantity));

                if (updated == 0)
                {
                    var exists = await _context.Products.AnyAsync(p => p.Id == line.ProductId);
                    if (!exists)
                    {
                        throw new KeyNotFoundException($"Product {line.ProductId} not found.");
                    }

                    throw new InvalidOperationException($"Insufficient stock for product {line.ProductId}.");
                }
            }

            await transaction.CommitAsync();
        }

        public async Task ReleaseStock(IEnumerable<StockItemDto> items)
        {
            var lines = MergeStockItems(items);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var line in lines)
            {
                // A product deleted since the order was placed is skipped: there is nothing to restock.
                await _context.Products
                    .Where(p => p.Id == line.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + line.Quantity));
            }

            await transaction.CommitAsync();
        }

        private static List<StockItemDto> MergeStockItems(IEnumerable<StockItemDto> items)
        {
            var lines = items
                .GroupBy(i => i.ProductId)
                .Select(g => new StockItemDto { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderBy(i => i.ProductId) // consistent lock order avoids deadlocks between concurrent reservations
                .ToList();

            if (lines.Count == 0)
            {
                throw new ArgumentException("At least one item is required.");
            }

            if (lines.Any(l => l.ProductId <= 0 || l.Quantity <= 0))
            {
                throw new ArgumentException("ProductId and Quantity must be greater than zero.");
            }

            return lines;
        }

        private Task<List<ProductDto>> FilterProductsInternal(decimal? minPrice, decimal? maxPrice, int? categoryId)
        {
            IQueryable<Product> products = _context.Products.AsNoTracking();

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value);
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            return ToProductDtos(products).ToListAsync();
        }

        private async Task EnsureCategoryExistsAsync(int categoryId)
        {
            if (!await _context.Categories.AnyAsync(c => c.Id == categoryId))
            {
                throw new KeyNotFoundException("Invalid category ID.");
            }
        }

        private IQueryable<ProductDto> ToProductDtos(IQueryable<Product> products)
        {
            return from p in products
                   join c in _context.Categories.AsNoTracking() on p.CategoryId equals c.Id into categories
                   from c in categories.DefaultIfEmpty()
                   select new ProductDto
                   {
                       Id = p.Id,
                       Name = p.Name,
                       Description = p.Description,
                       ImageUrl = p.ImageUrl,
                       Price = p.Price,
                       StockQuantity = p.StockQuantity,
                       CreatedAt = p.CreatedAt,
                       UpdatedAt = p.UpdatedAt,
                       CategoryId = p.CategoryId,
                       CategoryName = c != null ? c.CategoryName : string.Empty
                   };
        }
    }
}
