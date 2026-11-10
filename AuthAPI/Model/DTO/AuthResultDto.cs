namespace AuthAPI.Model.DTO
{
    public class AuthResultDto
    {
        // Encrypted (JWE) access token: opaque to the client, send it back as "Authorization: Bearer <token>".
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        // Returned alongside the token so the client never needs to decode it.
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}
