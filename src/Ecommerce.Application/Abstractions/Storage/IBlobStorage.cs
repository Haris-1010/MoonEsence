using System.IO;

namespace Ecommerce.Application.Abstractions.Storage;

public interface IBlobStorage
{
    Task<string?> UploadAsync(Stream stream, string fileName, string? contentType, string folder, CancellationToken cancellationToken = default);
}

