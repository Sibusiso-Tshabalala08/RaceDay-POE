using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores uploaded images in Azure Blob Storage and returns the public URL
/// of the stored blob. Only this URL is saved in SQL Server.
/// </summary>
public interface IBlobStorageService
{
    /// <param name="content">The image bytes.</param>
    /// <param name="contentType">e.g. image/png - stored so browsers render the blob as an image.</param>
    /// <param name="extension">e.g. ".png".</param>
    /// <param name="folder">Virtual folder inside the container, e.g. "event-banners".</param>
    Task<string> UploadImageAsync(Stream content, string contentType, string extension,
        string folder, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when no Azure connection string has been configured.</summary>
public class BlobStorageNotConfiguredException : Exception
{
    public BlobStorageNotConfiguredException()
        : base("Azure Blob Storage is not configured (AzureStorage:ConnectionString is empty).") { }
}

/// <summary>Thrown when Azure rejects or cannot complete the upload.</summary>
public class BlobStorageUnavailableException : Exception
{
    public BlobStorageUnavailableException(string message, Exception inner) : base(message, inner) { }
}

public class BlobStorageService : IBlobStorageService
{
    private readonly string? _connectionString;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;
    private volatile bool _containerReady;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"];
        _containerName = (configuration["AzureStorage:ContainerName"] ?? "raceday-images").ToLowerInvariant();
        _logger = logger;
    }

    public async Task<string> UploadImageAsync(Stream content, string contentType, string extension,
        string folder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new BlobStorageNotConfiguredException();

        try
        {
            var container = new BlobContainerClient(_connectionString, _containerName);

            if (!_containerReady)
            {
                // Public read access at blob level, so the browser can show the
                // image straight from its URL (the account must allow it).
                await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
                _containerReady = true;
            }

            // A random name means two uploads can never overwrite each other.
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";
            var blob = container.GetBlobClient(blobName);

            await blob.UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                cancellationToken);

            return blob.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            // PublicAccessNotPermitted means the storage account has anonymous
            // blob access switched off - see the README's Azure setup steps.
            _logger.LogError(ex, "Azure Blob Storage upload failed ({ErrorCode}).", ex.ErrorCode);
            throw new BlobStorageUnavailableException("Azure Blob Storage rejected the upload.", ex);
        }
    }
}
