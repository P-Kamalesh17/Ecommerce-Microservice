using AuthAPI.Model.DTO;

namespace AuthAPI.Services.IServices
{
    public interface IAuthService
    {
        Task<AuthResultDto> Register(RegisterDto registerDto);
        Task<AuthResultDto> Login(LoginDto loginDto);
        Task<AuthResultDto> Refresh(RefreshTokenRequestDto refreshTokenRequestDto);
        Task Logout(RefreshTokenRequestDto refreshTokenRequestDto);
        Task<UserProfileDto> GetProfile(string userId);
    }
}
