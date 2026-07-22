namespace RestaurantERP.Web.Services;

public interface IUploadStorageService
{
    string UploadsRoot { get; }
    Task<(string? Url, string? Error)> SaveImageAsync(IFormFile file, string subfolder, long maxBytes = 5 * 1024 * 1024);
    Task DeleteIfExistsAsync(string? publicUrl);
}

/// <summary>
/// Stores uploads under App_Data/images (NOT wwwroot) so file writes do not restart the app under dotnet watch / VS Hot Reload.
/// Public URLs remain /images/{subfolder}/{file}.
/// </summary>
public class UploadStorageService : IUploadStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadStorageService> _logger;

    public UploadStorageService(IWebHostEnvironment env, ILogger<UploadStorageService> logger)
    {
        _env = env;
        _logger = logger;

        UploadsRoot = Path.Combine(_env.ContentRootPath, "App_Data", "images");
        Directory.CreateDirectory(UploadsRoot);
    }

    public string UploadsRoot { get; }

    public async Task<(string? Url, string? Error)> SaveImageAsync(IFormFile file, string subfolder, long maxBytes = 5 * 1024 * 1024)
    {
        if (file == null || file.Length == 0)
            return (null, "No file selected.");

        if (file.Length > maxBytes)
            return (null, $"File must be under {maxBytes / (1024 * 1024)} MB.");

        var ext = ResolveImageExtension(file);
        if (ext == null)
            return (null, "Invalid image. Use JPG, PNG, WEBP or GIF.");

        var safeSubfolder = SanitizeSegment(subfolder);
        var dir = Path.Combine(UploadsRoot, safeSubfolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        try
        {
            await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await file.CopyToAsync(stream);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save upload to {Path}", fullPath);
            return (null, "Could not save file. Please try again.");
        }

        return ($"/images/{safeSubfolder}/{fileName}", null);
    }

    public Task DeleteIfExistsAsync(string? publicUrl)
    {
        if (string.IsNullOrEmpty(publicUrl))
            return Task.CompletedTask;

        var pathOnly = publicUrl.Split('?', 2)[0];

        if (pathOnly.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = pathOnly["/images/".Length..].Replace('/', Path.DirectorySeparatorChar);
            TryDelete(Path.Combine(UploadsRoot, relative));

            // Legacy files previously saved under wwwroot/images
            if (!string.IsNullOrEmpty(_env.WebRootPath))
                TryDelete(Path.Combine(_env.WebRootPath, "images", relative));

            return Task.CompletedTask;
        }

        if (pathOnly.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var relative = pathOnly.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            TryDelete(Path.Combine(_env.ContentRootPath, "App_Data", relative));
            if (!string.IsNullOrEmpty(_env.WebRootPath))
                TryDelete(Path.Combine(_env.WebRootPath, relative));
        }

        return Task.CompletedTask;
    }

    private static string SanitizeSegment(string segment)
    {
        var cleaned = string.Concat((segment ?? "misc").Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        return string.IsNullOrWhiteSpace(cleaned) ? "misc" : cleaned.ToLowerInvariant();
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
