using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Model
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
