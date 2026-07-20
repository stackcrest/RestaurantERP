namespace RestaurantERP.Web.Services;

public interface IUploadStorageService
{
    string UploadsRoot { get; }
    Task<(string? Url, string? Error)> SaveImageAsync(IFormFile file, string subfolder, long maxBytes = 5 * 1024 * 1024);
    Task DeleteIfExistsAsync(string? publicUrl);
}

public class UploadStorageService : IUploadStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadStorageService> _logger;

    public UploadStorageService(IWebHostEnvironment env, ILogger<UploadStorageService> logger)
    {
        _env = env;
        _logger = logger;
        UploadsRoot = Path.Combine(_env.ContentRootPath, "App_Data", "uploads");
        Directory.CreateDirectory(UploadsRoot);
    }

    public string UploadsRoot { get; }

    public async Task<(string? Url, string? Error)> SaveImageAsync(IFormFile file, string subfolder, long maxBytes = 5 * 1024 * 1024)
    {
        if (file.Length == 0)
            return (null, "No file selected.");

        if (file.Length > maxBytes)
            return (null, $"File must be under {maxBytes / (1024 * 1024)} MB.");

        var ext = ResolveImageExtension(file);
        if (ext == null)
            return (null, "Invalid image. Use JPG, PNG, WEBP or GIF.");

        var dir = Path.Combine(UploadsRoot, subfolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        try
        {
            await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save upload to {Path}", fullPath);
            return (null, "Could not save file. Please try again.");
        }

        return ($"/uploads/{subfolder}/{fileName}", null);
    }

    public Task DeleteIfExistsAsync(string? publicUrl)
    {
        if (string.IsNullOrEmpty(publicUrl) || !publicUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relative = publicUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

        // App_Data/uploads/...
        var appDataPath = Path.Combine(_env.ContentRootPath, "App_Data", relative);
        TryDelete(appDataPath);

        // Legacy wwwroot/uploads/...
        if (!string.IsNullOrEmpty(_env.WebRootPath))
        {
            var wwwPath = Path.Combine(_env.WebRootPath, relative);
            TryDelete(wwwPath);
        }

        return Task.CompletedTask;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Ignore delete failures for old files
        }
    }

    private static string? ResolveImageExtension(IFormFile file)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        var ext = Path.GetExtension(file.FileName);
        if (!string.IsNullOrEmpty(ext) && allowed.Contains(ext))
            return ext.ToLowerInvariant();

        return file.ContentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => null
        };
    }
}
