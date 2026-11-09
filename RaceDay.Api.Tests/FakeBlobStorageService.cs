using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Stand-in for Azure Blob Storage in tests: nothing is uploaded anywhere,
/// it just returns a predictable URL, so the upload endpoints can be tested
/// without an Azure account or network access.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(Stream content, string contentType, string extension,
        string folder, CancellationToken cancellationToken = default)
    {
        var url = $"https://fake.blob.core.windows.net/raceday-images/{folder}/{Guid.NewGuid():N}{extension}";
        return Task.FromResult(url);
    }
}
