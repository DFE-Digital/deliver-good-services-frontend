using Azure.Storage.Blobs;

namespace ServiceManual.Services;

/// <summary>
/// Fetches blob size and last-modified from Azure Blob Storage for URLs that match the configured base (e.g. CMS media storage).
/// </summary>
public class AzureBlobMetadataProvider : IBlobMetadataProvider
{
    private readonly string? _connectionString;
    private readonly string? _baseUrl;

    public AzureBlobMetadataProvider(IConfiguration configuration)
    {
        _connectionString = configuration["BlobStorage:ConnectionString"]?.Trim();
        _baseUrl = configuration["BlobStorage:BaseUrl"]?.TrimEnd('/');
    }

    public async Task<BlobMetadata?> GetMetadataAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_connectionString) || string.IsNullOrEmpty(_baseUrl))
            return null;

        if (string.IsNullOrWhiteSpace(blobUrl) || !blobUrl.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            // URL format: https://account.blob.core.windows.net/container/path/to/blob
            var path = new Uri(blobUrl).AbsolutePath.TrimStart('/');
            var firstSlash = path.IndexOf('/');
            if (firstSlash <= 0)
                return null;

            var containerName = path[..firstSlash];
            var blobName = path[(firstSlash + 1)..];
            if (string.IsNullOrEmpty(blobName))
                return null;

            var client = new BlobClient(_connectionString, containerName, blobName);
            var response = await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var props = response.Value;
            return new BlobMetadata(props.ContentLength, props.LastModified);
        }
        catch
        {
            return null;
        }
    }
}
