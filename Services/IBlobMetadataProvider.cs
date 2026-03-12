namespace ServiceManual.Services;

/// <summary>
/// Provides file metadata (size, last modified) for blob storage URLs used in document download blocks.
/// When not configured or for non-blob URLs, returns null and the UI shows type label only.
/// </summary>
public interface IBlobMetadataProvider
{
    /// <summary>
    /// Gets blob metadata for the given URL if it points to the configured storage; otherwise null.
    /// </summary>
    Task<BlobMetadata?> GetMetadataAsync(string blobUrl, CancellationToken cancellationToken = default);
}

public record BlobMetadata(long SizeBytes, DateTimeOffset LastModified);
