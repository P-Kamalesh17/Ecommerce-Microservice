using ProductAPI.Services.IServices;

namespace ProductAPI.Services
{
    public class ImageStorageService : IImageStorageService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private const string RelativeFolder = "images/products";

        private readonly IWebHostEnvironment _environment;

        public ImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file.Length == 0)
            {
                throw new ArgumentException("Image file is empty.", nameof(file));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("Image file must be 5 MB or smaller.", nameof(file));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only .jpg, .jpeg, .png, .gif and .webp images are allowed.", nameof(file));
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var folderPath = Path.Combine(webRoot, RelativeFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/{RelativeFolder}/{fileName}";
        }

        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
