using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Model.DTO;
using ProductAPI.Services.IServices;

namespace ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("get-all-category")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategories();
                return StatusCode(StatusCodes.Status200OK, categories);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while retrieving categories." });
            }
        }

        [HttpGet("get-category-by-id/{categoryid}")]
        public async Task<IActionResult> GetCategoryById(int categoryid)
        {
            try
            {
                var category = await _categoryService.GetCategoryById(categoryid);

                if (category == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Category not found." });
                }

                return StatusCode(StatusCodes.Status200OK, category);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while retrieving the category." });
            }
        }

        [HttpPost("create-category")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto categoryRequestDto)
        {
            try
            {
                var category = await _categoryService.CreateCategory(categoryRequestDto);
                return StatusCode(StatusCodes.Status201Created, category);
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Could not save the category. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while creating the category." });
            }
        }

        [HttpPut("update-category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequestDto categoryRequestDto)
        {
            try
            {
                var category = await _categoryService.UpdateCategoryById(id, categoryRequestDto);

                if (category == null)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Category not found." });
                }

                return StatusCode(StatusCodes.Status200OK, category);
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Could not update the category. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while updating the category." });
            }
        }

        [HttpDelete("delete-category-by-id/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var deleted = await _categoryService.DeleteCategoryById(id);

                if (!deleted)
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "Category not found." });
                }

                return StatusCode(StatusCodes.Status200OK, new { message = "Category deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status409Conflict, new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Could not delete the category. Please try again." });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while deleting the category." });
            }
        }
    }
}
