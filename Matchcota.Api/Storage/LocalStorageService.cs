using Matchcota.Services.Dogs;

namespace Matchcota.Api.Storage;

public sealed class LocalStorageService(IWebHostEnvironment webHostEnvironment) : IStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        var sanitizedExtension = extension.Trim().TrimStart('.').ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}.{sanitizedExtension}";

        var uploadsDirectory = Path.Combine(_webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsDirectory);

        var physicalPath = Path.Combine(uploadsDirectory, fileName);

        await using var fileStream = File.Create(physicalPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/{fileName}";
    }
}
