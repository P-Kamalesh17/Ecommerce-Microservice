namespace ProductAPI.Services.IServices
{
    public interface IImageStorageService
    {
        Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);

        void DeleteImage(string imageUrl);
    }
}
