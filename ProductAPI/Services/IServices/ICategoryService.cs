using ProductAPI.Model.DTO;

namespace ProductAPI.Services.IServices
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllCategories();
        Task<CategoryDto?> GetCategoryById(int categoryId);
        Task<CategoryDto> CreateCategory(CategoryRequestDto categoryRequestDto);
        Task<CategoryDto?> UpdateCategoryById(int categoryId, CategoryRequestDto categoryRequestDto);
        Task<bool> DeleteCategoryById(int categoryId);
    }
}
