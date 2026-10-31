using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>Azure Blob Storage implementation (works with Azurite locally and with a real storage account).</summary>
public class BlobFileStorage : IFileStorage
{
    private readonly BlobContainerClient _container;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _containerReady;

    public BlobFileStorage(BlobServiceClient service, IConfiguration config)
    {
        var name = config["AzureStorage:ContainerName"] ?? "raceday-images";
        _container = service.GetBlobContainerClient(name);
    }

    public async Task<string> SaveAsync(string folder, Stream content, string contentType, string extension,
        CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
        var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await _container.GetBlobClient(blobName).UploadAsync(content, options, ct);
        return blobName;
    }

    public async Task<StoredFile?> OpenReadAsync(string blobName, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        try
        {
            var response = await _container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: ct);
            return new StoredFile(response.Value.Content, response.Value.Details.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct);
        await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
    }

    // The container is created on first use (private - images are only served through the API),
    // so the API can start even if storage is still booting.
    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_containerReady) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_containerReady) return;
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            _containerReady = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
