using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthAPI.Data;
using AuthAPI.Model;
using AuthAPI.Model.DTO;
using AuthAPI.Services.IServices;
using ECommerce.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.Services
{
    public class AuthService : IAuthService
    {
        private const string DefaultRole = UserRoles.Customer;

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResultDto> Register(RegisterDto registerDto)
        {
            if (registerDto == null)
            {
                throw new ArgumentNullException(
                    nameof(registerDto),
                    "Registration details are required."
                );
            }

            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                throw new ArgumentException("A user with this email already exists.");
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                PhoneNumber = string.IsNullOrWhiteSpace(registerDto.PhoneNumber) ? null : registerDto.PhoneNumber.Trim(),
                Address = registerDto.Address
            };

            var createResult = await _userManager.CreateAsync(user, registerDto.Password);
            if (!createResult.Succeeded)
            {
                throw new ArgumentException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
            }

            if (!await _roleManager.RoleExistsAsync(DefaultRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(DefaultRole));
            }

            await _userManager.AddToRoleAsync(user, DefaultRole);

            return await IssueTokens(user);
        }

        public async Task<UserProfileDto> GetProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("User not found.");

            return new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };
        }

        public async Task<AuthResultDto> Login(LoginDto loginDto)
        {
            if (loginDto == null)
            {
                throw new ArgumentNullException(
                    nameof(loginDto),
                    "Login details are required."
                );
            }

            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException("Account is temporarily locked. Please try again later.");
            }

            if (!await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                await _userManager.AccessFailedAsync(user);
                _logger.LogWarning("Failed login attempt for user {UserId}.", user.Id);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            return await IssueTokens(user);
        }

        public async Task<AuthResultDto> Refresh(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            if (refreshTokenRequestDto == null)
            {
                throw new ArgumentNullException(
                    nameof(refreshTokenRequestDto),
                    "Refresh token is required."
                );
            }

            var tokenHash = HashToken(refreshTokenRequestDto.RefreshToken);

            var storedToken = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (storedToken.RevokedAt != null)
            {
                _logger.LogWarning("Revoked refresh token reused for user {UserId}. Revoking all active tokens.", storedToken.UserId);
                await RevokeAllActiveTokens(storedToken.UserId);
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
            }

            var (rawRefreshToken, newHash) = CreateRefreshTokenValue();

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByTokenHash = newHash;

            var newToken = NewRefreshTokenEntity(storedToken.UserId, newHash);
            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync();

            var accessToken = await CreateAccessToken(storedToken.User);

            return new AuthResultDto
            {
                Token = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                RefreshToken = rawRefreshToken,
                UserId = storedToken.User.Id,
                Email = storedToken.User.Email ?? string.Empty,
                Roles = accessToken.Roles
            };
        }

        public async Task Logout(RefreshTokenRequestDto refreshTokenRequestDto)
        {
            if (refreshTokenRequestDto == null)
            {
                throw new ArgumentNullException(
                    nameof(refreshTokenRequestDto),
                    "Refresh token is required."
                );
            }

            var tokenHash = HashToken(refreshTokenRequestDto.RefreshToken);

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken != null && storedToken.RevokedAt == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<AuthResultDto> IssueTokens(User user)
        {
            var accessToken = await CreateAccessToken(user);

            var (rawRefreshToken, hash) = CreateRefreshTokenValue();
            _context.RefreshTokens.Add(NewRefreshTokenEntity(user.Id, hash));
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Token = accessToken.Token,
                ExpiresAt = accessToken.ExpiresAt,
                RefreshToken = rawRefreshToken,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = accessToken.Roles
            };
        }

        private async Task<(string Token, DateTime ExpiresAt, List<string> Roles)> CreateAccessToken(User user)
        {
            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured.");
            }

            var encryptionKey = GetEncryptionKey();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var minutes = _configuration.GetValue("Jwt:AccessTokenMinutes", 15);
            var expires = DateTime.UtcNow.AddMinutes(minutes);

            // Sign first (HS256 with Jwt:Key), then encrypt the signed token (JWE: alg dir, enc A256CBC-HS512
            // with Jwt:EncryptionKey). The payload is unreadable without the encryption key.
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256),
                EncryptingCredentials = new EncryptingCredentials(
                    new SymmetricSecurityKey(encryptionKey),
                    JwtConstants.DirectKeyUseAlg,
                    SecurityAlgorithms.Aes256CbcHmacSha512)
            };

            var token = new JsonWebTokenHandler().CreateToken(descriptor);

            return (token, expires, roles);
        }

        private byte[] GetEncryptionKey()
        {
            var value = _configuration["Jwt:EncryptionKey"];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Jwt:EncryptionKey is not configured.");
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Jwt:EncryptionKey must be a Base64 string (check for stray spaces or line breaks).");
            }

            if (bytes.Length != ECommerce.Shared.Extensions.AuthenticationExtensions.EncryptionKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:EncryptionKey must decode to {ECommerce.Shared.Extensions.AuthenticationExtensions.EncryptionKeyBytes} bytes but decodes to {bytes.Length}.");
            }

            return bytes;
        }

        private RefreshToken NewRefreshTokenEntity(string userId, string tokenHash)
        {
            var days = _configuration.GetValue("Jwt:RefreshTokenDays", 7);

            return new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(days)
            };
        }

        private async Task RevokeAllActiveTokens(string userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static (string Raw, string Hash) CreateRefreshTokenValue()
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            return (raw, HashToken(raw));
        }

        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
