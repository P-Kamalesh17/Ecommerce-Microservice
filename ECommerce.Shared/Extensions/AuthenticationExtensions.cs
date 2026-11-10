using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Shared.Extensions
{
    public static class AuthenticationExtensions
    {
        // A256CBC-HS512 needs a 512-bit (64-byte) key.
        public const int EncryptionKeyBytes = 64;

        // A compact JWE has 5 dot-separated parts (header.key.iv.ciphertext.tag); a plain signed JWT has 3.
        private const int JweSegmentCount = 5;

        /// <summary>
        /// Validates access tokens that are JWEs: encrypted with Jwt:EncryptionKey (alg dir, enc A256CBC-HS512)
        /// and containing a JWT signed with Jwt:Key. Plain signed tokens are rejected.
        /// </summary>
        public static WebApplicationBuilder AddJwtAuthentication(this WebApplicationBuilder builder,
            string secretConfigKey = "Jwt:Key",
            string issuerConfigKey = "Jwt:Issuer",
            string audienceConfigKey = "Jwt:Audience",
            string encryptionKeyConfigKey = "Jwt:EncryptionKey")
        {
            var secret = GetRequired(builder.Configuration, secretConfigKey);
            var issuer = GetRequired(builder.Configuration, issuerConfigKey);
            var audience = GetRequired(builder.Configuration, audienceConfigKey);
            var encryptionKey = GetEncryptionKey(builder.Configuration, encryptionKeyConfigKey);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                        // Decrypts the outer JWE; the inner signed JWT is then validated with IssuerSigningKey.
                        TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey),
                        // The role claims are written as ClaimTypes.Role by AuthAPI.
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        // Encrypted tokens only: refuse a plain signed token even if its signature is valid.
                        OnMessageReceived = context =>
                        {
                            var header = context.Request.Headers.Authorization.ToString();

                            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                var token = header["Bearer ".Length..].Trim();

                                if (token.Split('.').Length != JweSegmentCount)
                                {
                                    context.Fail("Only encrypted (JWE) access tokens are accepted.");
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            return builder;
        }

        private static byte[] GetEncryptionKey(IConfiguration configuration, string key)
        {
            var value = GetRequired(configuration, key);

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"JWT setting '{key}' must be a Base64 string (check for stray spaces or line breaks).");
            }

            if (bytes.Length != EncryptionKeyBytes)
            {
                throw new InvalidOperationException(
                    $"JWT setting '{key}' must decode to {EncryptionKeyBytes} bytes but decodes to {bytes.Length}.");
            }

            return bytes;
        }

        private static string GetRequired(IConfiguration configuration, string key)
        {
            var value = configuration.GetValue<string>(key);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"JWT setting '{key}' is not configured.");
            }

            return value;
        }
    }
}
