using Ecommerce.Application.Abstractions.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Storage
{
    public class CloudinaryBlobStorage : IBlobStorage
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryBlobStorage(IConfiguration config)
        {
            var account = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadAsync(Stream stream, string fileName, string? contentType, string folder, CancellationToken cancellationToken = default)
        {
            if (stream is null) return null;

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder // 👉 "products"
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString(); // 🔥 yahi DB me save hoga
        }
    }
}