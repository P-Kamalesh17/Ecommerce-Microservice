using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthAPI.Model.DTO;
using AuthAPI.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        // Profile of the logged-in user; the id comes from the token, never from the client.
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("The token does not contain a user id.");

                var profile = await _authService.GetProfile(userId);
                return StatusCode(StatusCodes.Status200OK, profile);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "retrieving the profile");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.Register(registerDto);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "registering the user");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.Login(loginDto);
                return StatusCode(StatusCodes.Status200OK, result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "logging in");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto refreshTokenRequestDto)
        {
            try
            {
                var result = await _authService.Refresh(refreshTokenRequestDto);
                return StatusCode(StatusCodes.Status200OK, result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "refreshing the token");
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto refreshTokenRequestDto)
        {
            try
            {
                await _authService.Logout(refreshTokenRequestDto);
                return StatusCode(StatusCodes.Status200OK, new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex, "logging out");
            }
        }

        private IActionResult ErrorResult(Exception ex, string operation)
        {
            return ex switch
            {
                ArgumentException => StatusCode(StatusCodes.Status400BadRequest, new { message = ex.Message }),
                UnauthorizedAccessException => StatusCode(StatusCodes.Status401Unauthorized, new { message = ex.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = $"An unexpected error occurred while {operation}." })
            };
        }
    }
}
