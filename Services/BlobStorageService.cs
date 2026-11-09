using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AssignmentProg.Services;

public class BlobStorageService
{
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };
    private readonly IConfiguration _cfg;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration cfg) => _cfg = cfg;
    
    private BlobContainerClient Container()
    {
        if (_container == null)
        {
            var c = new BlobContainerClient(_cfg["AzureBlob:ConnectionString"], _cfg["AzureBlob:Container"] ?? "event-banners");
            c.CreateIfNotExists(PublicAccessType.Blob);
            _container = c;
        }
        return _container;
    }

    /// Uploads to Azure and returns the public URL, or null if the file is not a valid image.
    public async Task<string?> UploadAsync(IFormFile file, string folder)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext) || file.Length == 0 || file.Length > 5 * 1024 * 1024) return null;

        var blob = Container().GetBlobClient($"{folder}/{Guid.NewGuid()}{ext}");
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
        return blob.Uri.ToString();
    }
}