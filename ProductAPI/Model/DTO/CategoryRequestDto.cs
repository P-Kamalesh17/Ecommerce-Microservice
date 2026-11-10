using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Model.DTO
{
    public class CategoryRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;
    }
}
