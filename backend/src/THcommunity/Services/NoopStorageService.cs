using THcommunity.Configuration;
using Microsoft.Extensions.Options;

namespace THcommunity.Services;
/// <summary>
/// A fallback storage implementation used when Cloudflare R2 (via the AWS S3 compatible client)
/// is not configured. This service logs a warning and throws an <see cref="InvalidOperationException"/>
/// for each operation to make it clear at runtime that storage is unavailable.
/// </summary>
/// <remarks>
/// Configure Cloudflare R2 by setting <c>Cloudflare:R2AccountId</c> and <c>Cloudflare:R2AccessKeyId</c>
/// in configuration (for example in <c>appsettings.Local.json</c>) to enable the real <see cref="StorageService"/>.
/// </remarks>
public class NoopStorageService : IStorageService
{
    private readonly ILogger<NoopStorageService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="NoopStorageService"/>.
    /// </summary>
    /// <param name="logger">Logger used to emit warnings when storage API is invoked.</param>
    public NoopStorageService(ILogger<NoopStorageService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> and logs a warning indicating
    /// that Cloudflare R2 is not configured. Intended as a clear failure mode for local
    /// development when real storage is unavailable.
    /// </summary>
    public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        _logger.LogWarning("Storage is not configured: Cloudflare R2 settings are missing. Upload attempted for {FileName}.", fileName);
        throw new InvalidOperationException("Cloudflare R2 is not configured. Set Cloudflare:R2AccountId and Cloudflare:R2AccessKeyId to enable storage.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> and logs a warning indicating
    /// that Cloudflare R2 is not configured.
    /// </summary>
    public Task DeleteFileAsync(string fileUrl)
    {
        _logger.LogWarning("Storage is not configured: Cloudflare R2 settings are missing. Delete attempted for {FileUrl}.", fileUrl);
        throw new InvalidOperationException("Cloudflare R2 is not configured. Set Cloudflare:R2AccountId and Cloudflare:R2AccessKeyId to enable storage.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> and logs a warning indicating
    /// that Cloudflare R2 is not configured. Returns no presigned URL.
    /// </summary>
    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry)
    {
        _logger.LogWarning("Storage is not configured: Cloudflare R2 settings are missing. Presigned URL requested for {Key}.", key);
        throw new InvalidOperationException("Cloudflare R2 is not configured. Set Cloudflare:R2AccountId and Cloudflare:R2AccessKeyId to enable storage.");
    }
}
