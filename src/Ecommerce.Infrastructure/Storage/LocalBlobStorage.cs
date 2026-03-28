using System.IO;
using Ecommerce.Application.Abstractions.Storage;

namespace Ecommerce.Infrastructure.Storage;

public class LocalBlobStorage : IBlobStorage
{
    private readonly string _webRootPath;
    private readonly string _rootRelative;

    public LocalBlobStorage(string webRootPath, string rootRelative)
    {
        _webRootPath = webRootPath;
        _rootRelative = rootRelative;
    }

    public async Task<string?> UploadAsync(Stream stream, string fileName, string? contentType, string folder, CancellationToken cancellationToken = default)
    {
        if (stream is null) return null;

        var uploadsRoot = Path.Combine(_webRootPath, _rootRelative, folder);
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ".img";

        var generatedFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, generatedFileName);

        await using var target = File.Create(fullPath);
        await stream.CopyToAsync(target, cancellationToken);

        // URL relative to app root
        var url = "/" + Path.Combine(_rootRelative, folder, generatedFileName).Replace('\\', '/').TrimStart('/');
        return url;
    }
}

