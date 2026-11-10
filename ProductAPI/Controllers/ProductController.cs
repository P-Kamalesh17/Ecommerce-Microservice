using ECommerce.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Model.DTO;
using ProductAPI.Services.IServices;

namespace ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("get-all-products")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _productService.GetAllProducts();
                return StatusCode(StatusCodes.Status200OK, products);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving products");
            }
        }

        [HttpGet("get-product-by-id/{id:int}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _productService.GetProductById(id);

                if (product == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Product not found." });
                }

                return StatusCode(StatusCodes.Status200OK, product);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the product");
            }
        }

        [HttpGet("get-by-category/{categoryId:int}")]
        public async Task<IActionResult> GetProductsByCategoryId(int categoryId)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryId(categoryId);
                return StatusCode(StatusCodes.Status200OK, products);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving products by category");
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterProducts([FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int? categoryId)
        {
            try
            {
                var products = await _productService.FilterProducts(minPrice, maxPrice, categoryId);
                return StatusCode(StatusCodes.Status200OK, products);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "filtering products");
            }
        }

        [HttpPost("create-product")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto createProductDto)
        {
            try
            {
                var product = await _productService.CreateProduct(createProductDto);
                return StatusCode(StatusCodes.Status201Created, product);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "creating the product");
            }
        }

        [HttpPut("update-product-by-id/{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] CreateProductDto createProductDto)
        {
            try
            {
                var product = await _productService.UpdateProductById(id, createProductDto);

                if (product == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Product not found." });
                }

                return StatusCode(StatusCodes.Status200OK, product);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "updating the product");
            }
        }

        [HttpDelete("delete-product-by-id/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var deleted = await _productService.DeleteProductById(id);

                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Product not found." });
                }

                return StatusCode(StatusCodes.Status200OK, new { message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "deleting the product");
            }
        }

        [HttpPost("reserve-stock")]
        [StaffOrInternalService]
        public async Task<IActionResult> ReserveStock([FromBody] List<StockItemDto> items)
        {
            try
            {
                await _productService.ReserveStock(items);
                return StatusCode(StatusCodes.Status200OK, new { message = "Stock reserved." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "reserving stock");
            }
        }

        [HttpPost("release-stock")]
        [StaffOrInternalService]
        public async Task<IActionResult> ReleaseStock([FromBody] List<StockItemDto> items)
        {
            try
            {
                await _productService.ReleaseStock(items);
                return StatusCode(StatusCodes.Status200OK, new { message = "Stock released." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "releasing stock");
            }
        }

        private IActionResult ErrorResult(Exception ex, string operation)
        {
            return ex switch
            {
                ArgumentException => StatusCode(StatusCodes.Status400BadRequest, new { message = ex.Message }),
                KeyNotFoundException => StatusCode(StatusCodes.Status404NotFound, new { message = ex.Message }),
                InvalidOperationException => StatusCode(StatusCodes.Status409Conflict, new { message = ex.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while {operation}." })
            };
        }
    }
}
