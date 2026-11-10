using OrderAPI.Model.DTO;

namespace OrderAPI.Services.IServices
{
    public interface IUserClient
    {
        /// <summary>Reads the caller's own profile from AuthAPI (their Authorization header is forwarded). Null if not found.</summary>
        Task<UserProfileDto?> GetProfileAsync(string authorizationHeader, CancellationToken cancellationToken = default);
    }
}
