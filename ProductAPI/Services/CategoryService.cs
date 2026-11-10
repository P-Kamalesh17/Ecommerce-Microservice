using DAL.Models;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Model.DTO;
using ProductAPI.Services.IServices;

namespace ProductAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<CategoryDto>> GetAllCategories()
        {
            return _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    CategoryId = c.Id,
                    CategoryName = c.CategoryName
                })
                .ToListAsync();
        }

        public Task<CategoryDto?> GetCategoryById(int categoryId)
        {
            return _context.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.Id,
                    CategoryName = c.CategoryName
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CategoryDto> CreateCategory(CategoryRequestDto categoryRequestDto)
        {
            var category = new Category { CategoryName = categoryRequestDto.CategoryName };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return ToDto(category);
        }

        public async Task<CategoryDto?> UpdateCategoryById(int categoryId, CategoryRequestDto categoryRequestDto)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return null;
            }

            category.CategoryName = categoryRequestDto.CategoryName;
            await _context.SaveChangesAsync();

            return ToDto(category);
        }

        public async Task<bool> DeleteCategoryById(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return false;
            }

            if (await _context.Products.AnyAsync(p => p.CategoryId == categoryId))
            {
                throw new InvalidOperationException("Category cannot be deleted because products are assigned to it.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        private static CategoryDto ToDto(Category category)
        {
            return new CategoryDto
            {
                CategoryId = category.Id,
                CategoryName = category.CategoryName
            };
        }
    }
}
