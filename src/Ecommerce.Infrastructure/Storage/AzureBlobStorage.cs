using System.Net.Mime;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Ecommerce.Application.Abstractions.Storage;

namespace Ecommerce.Infrastructure.Storage;

public class AzureBlobStorage : IBlobStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorage(string connectionString, string containerName)
    {
        var serviceClient = new BlobServiceClient(connectionString);
        _container = serviceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string?> UploadAsync(Stream stream, string fileName, string? contentType, string folder, CancellationToken cancellationToken = default)
    {
        if (stream is null) return null;

        // Ensure container exists (no-op if already present, depends on account permissions).
        try
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            // If permissions don't allow container creation, continue and attempt upload.
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".img";

        var blobName = $"{folder.TrimEnd('/')}/{Guid.NewGuid():N}{ext}";
        var blobClient = _container.GetBlobClient(blobName);

        var finalContentType = contentType;
        if (string.IsNullOrWhiteSpace(finalContentType))
            finalContentType = GetContentTypeFromExtension(ext);

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = finalContentType }
            },
            cancellationToken);

        // Requires the blob/container to allow public read, otherwise the URL won't work.
        return blobClient.Uri.ToString();
    }

    private static string GetContentTypeFromExtension(string ext)
    {
        ext = ext.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}

