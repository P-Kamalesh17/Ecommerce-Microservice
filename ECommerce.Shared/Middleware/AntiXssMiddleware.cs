using System.Net;
using System.Text;
using System.Text.Json;
using Ganss.Xss;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Shared.Middleware
{
    /// <summary>
    /// Rejects (400) any request whose query string, JSON body or form fields contain HTML or script,
    /// using Ganss.Xss (HtmlSanitizer) with no tags or attributes allowed.
    /// </summary>
    public class AntiXssMiddleware
    {
        private const int BufferThreshold = 64 * 1024;
        private const int BufferLimit = 32 * 1024 * 1024;

        // Must reach the service exactly as sent, so they are never scanned.
        private static readonly HashSet<string> SkippedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "ConfirmPassword", "RefreshToken", "Token"
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;

        public AntiXssMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // No tags and no attributes allowed: anything the sanitizer would change is markup.
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedAttributes.Clear();

            if (await ContainsMarkupAsync(context, sanitizer))
            {
                await RespondWithAnError(context);
                return;
            }

            await _next(context);
        }

        private static async Task<bool> ContainsMarkupAsync(HttpContext context, HtmlSanitizer sanitizer)
        {
            var request = context.Request;

            foreach (var pair in request.Query)
            {
                if (SkippedNames.Contains(pair.Key))
                {
                    continue;
                }

                foreach (var value in pair.Value)
                {
                    if (IsUnsafe(sanitizer, value))
                    {
                        return true;
                    }
                }
            }

            if (request.HasJsonContentType())
            {
                // Buffer the body so the controller can still read it after this middleware.
                request.EnableBuffering(BufferThreshold, BufferLimit);

                string body;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync(context.RequestAborted);
                }

                request.Body.Position = 0;

                try
                {
                    using var document = JsonDocument.Parse(body);
                    return ElementContainsMarkup(document.RootElement, sanitizer);
                }
                catch (JsonException)
                {
                    // Not valid JSON: the framework answers with its own 400.
                    return false;
                }
            }

            if (request.HasFormContentType)
            {
                request.EnableBuffering(BufferThreshold, BufferLimit);

                try
                {
                    var form = await request.ReadFormAsync(context.RequestAborted);
                    request.Body.Position = 0;

                    // Text fields only: uploaded files (form.Files) are never read here.
                    foreach (var field in form)
                    {
                        if (SkippedNames.Contains(field.Key))
                        {
                            continue;
                        }

                        foreach (var value in field.Value)
                        {
                            if (IsUnsafe(sanitizer, value))
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (InvalidDataException)
                {
                    return false;
                }
            }

            return false;
        }

        private static bool ElementContainsMarkup(JsonElement element, HtmlSanitizer sanitizer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return IsUnsafe(sanitizer, element.GetString());

                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (SkippedNames.Contains(property.Name))
                        {
                            continue;
                        }

                        if (ElementContainsMarkup(property.Value, sanitizer))
                        {
                            return true;
                        }
                    }

                    return false;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (ElementContainsMarkup(item, sanitizer))
                        {
                            return true;
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }

        private static bool IsUnsafe(HtmlSanitizer sanitizer, string? value)
        {
            // Without a "<" there is no tag or comment, so plain text such as "Tom & Jerry" is never flagged.
            if (string.IsNullOrEmpty(value) || value.IndexOf('<') < 0)
            {
                return false;
            }

            // If the sanitizer changes anything, the input contained markup ("5 < 10" comes back unchanged).
            var sanitized = sanitizer.Sanitize(value);

            return !string.Equals(Normalize(sanitized), Normalize(value), StringComparison.Ordinal);
        }

        private static string Normalize(string text)
        {
            return WebUtility.HtmlDecode(text).Replace("\r\n", "\n");
        }

        private static Task RespondWithAnError(HttpContext context)
        {
            var error = new ErrorResponse
            {
                ErrorCode = StatusCodes.Status400BadRequest,
                Description = "Potentially dangerous input (HTML or script) was found in the request.",
                Message = "Potentially dangerous input (HTML or script) was found in the request."
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return context.Response.WriteAsJsonAsync(error, JsonOptions);
        }
    }

    public static class AntiXssMiddlewareExtension
    {
        public static IApplicationBuilder UseAntiXssMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiXssMiddleware>();
        }
    }

    public class ErrorResponse
    {
        public int ErrorCode { get; set; }
        public string Description { get; set; } = string.Empty;

        // Same text as Description, so clients that read { message } (like the other API errors) keep working.
        public string Message { get; set; } = string.Empty;
    }
}
