using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Freshwalk.Application;
using Freshwalk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Freshwalk.Infrastructure.Services;

public class CloudinaryService(IOptions<CloudinaryOptions> options) : ICloudinaryService
{
    private static readonly string[] VideoExtensions = [".mp4", ".mov", ".webm"];

    private Cloudinary CreateClient()
    {
        var cfg = options.Value;
        var account = new Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret);
        return new Cloudinary(account) { Api = { Secure = true } };
    }

    public async Task<CloudinaryUploadResult> UploadAsync(
        Stream stream, string fileName, string folder, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (VideoExtensions.Contains(ext))
            {
                var p = new VideoUploadParams
                {
                    File            = new FileDescription(fileName, stream),
                    Folder          = folder,
                    UniqueFilename  = true,
                    Overwrite       = false,
                };
                var r = await client.UploadAsync(p);
                return r.Error is not null
                    ? new CloudinaryUploadResult(false, null, null, r.Error.Message)
                    : new CloudinaryUploadResult(true, r.SecureUrl?.ToString(), r.PublicId, null);
            }
            else
            {
                var p = new ImageUploadParams
                {
                    File            = new FileDescription(fileName, stream),
                    Folder          = folder,
                    UniqueFilename  = true,
                    Overwrite       = false,
                };
                var r = await client.UploadAsync(p);
                return r.Error is not null
                    ? new CloudinaryUploadResult(false, null, null, r.Error.Message)
                    : new CloudinaryUploadResult(true, r.SecureUrl?.ToString(), r.PublicId, null);
            }
        }
        catch (Exception ex)
        {
            return new CloudinaryUploadResult(false, null, null, ex.Message);
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct = default)
    {
        try
        {
            var client = CreateClient();
            await client.DestroyAsync(new DeletionParams(publicId));
        }
        catch
        {
            // Non-fatal — DB record is still removed even if Cloudinary delete fails
        }
    }
}
