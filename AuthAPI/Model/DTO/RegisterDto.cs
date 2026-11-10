using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model.DTO
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }
    }
}
