using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model.DTO
{
    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
