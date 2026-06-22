using Amazon.S3;
using Amazon.S3.Model;
using THcommunity.Configuration;
using Microsoft.Extensions.Options;

namespace THcommunity.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string fileUrl);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry);
}

public class StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;
    private readonly ILogger<StorageService> _logger;

    public StorageService(IAmazonS3 s3Client, IOptions<AppSettings> settings, ILogger<StorageService> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        
        var cloudflareSettings = settings.Value.Cloudflare;
        _bucketName = cloudflareSettings.R2BucketName;
        _publicUrl = cloudflareSettings.R2PublicUrl;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var key = $"uploads/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}/{fileName}";
        
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request);
        
        return $"{_publicUrl}/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl) || !fileUrl.StartsWith(_publicUrl))
            return;

        var key = fileUrl.Replace($"{_publicUrl}/", "");
        
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public async Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }
}
