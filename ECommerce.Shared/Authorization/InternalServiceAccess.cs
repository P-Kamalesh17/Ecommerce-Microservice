using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Authorization
{
    /// <summary>
    /// Shared secret that lets one backend service call another service's internal endpoints
    /// (for example OrderAPI taking stock from ProductAPI) without a user token. The browser never has it.
    /// </summary>
    public static class InternalServiceKey
    {
        public const string HeaderName = "X-Internal-Key";
        public const string ConfigKey = "Internal:ApiKey";

        /// <summary>The configured key, trimmed: an HTTP header value is trimmed too, so stray whitespace would never match.</summary>
        public static string? Get(IConfiguration configuration) => configuration[ConfigKey]?.Trim();

        /// <summary>Fails fast at startup when the key is missing.</summary>
        public static string Require(IConfiguration configuration)
        {
            var value = Get(configuration);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Setting '{ConfigKey}' is not configured (use user-secrets or the Internal__ApiKey environment variable).");
            }

            return value;
        }

        public static bool Matches(string? supplied, string? expected)
        {
            if (string.IsNullOrWhiteSpace(supplied) || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(supplied.Trim()),
                Encoding.UTF8.GetBytes(expected));
        }
    }

    /// <summary>
    /// Allows a call that carries the internal service key, or a logged-in Admin / Shipment user.
    /// Anyone else gets 401 (not logged in) or 403 (wrong role).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class StaffOrInternalServiceAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;
            var expected = InternalServiceKey.Get(http.RequestServices.GetRequiredService<IConfiguration>());

            if (InternalServiceKey.Matches(http.Request.Headers[InternalServiceKey.HeaderName].ToString(), expected))
            {
                return;
            }

            var user = http.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!user.IsInRole(UserRoles.Admin) && !user.IsInRole(UserRoles.Shipment))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }
    }
}
