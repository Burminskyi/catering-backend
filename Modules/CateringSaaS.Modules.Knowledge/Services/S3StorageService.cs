using Amazon.S3;
using Amazon.S3.Model;
using CateringSaaS.Modules.Knowledge.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CateringSaaS.Modules.Knowledge.Services;

public sealed class S3StorageService : IStorageService, IDisposable
{
    private readonly S3Options _options;
    private readonly ILogger<S3StorageService> _logger;
    private readonly object _gate = new();
    private IAmazonS3? _client;
    private bool _ownsClient;

    public S3StorageService(IOptions<S3Options> options, ILogger<S3StorageService> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        EnsureConfigured();

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("WorkspaceId is required.", nameof(workspaceId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var safeName = SanitizeFileName(fileName);
        var key = $"workspaces/{workspaceId:D}/documents/{Guid.NewGuid():N}_{safeName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        await GetClient().PutObjectAsync(request, cancellationToken);
        _logger.LogInformation("Uploaded knowledge file to R2 key {ObjectKey}", key);
        return key;
    }

    public async Task DeleteFileAsync(string fileUrlOrKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrKey))
        {
            return;
        }

        EnsureConfigured();

        var key = ExtractObjectKey(fileUrlOrKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        try
        {
            await GetClient().DeleteObjectAsync(_options.BucketName, key, cancellationToken);
            _logger.LogInformation("Deleted knowledge file from R2 key {ObjectKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete knowledge file from R2 key {ObjectKey}", key);
            throw;
        }
    }

    public string GetDownloadUrl(string fileUrlOrKey, TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(fileUrlOrKey))
        {
            return string.Empty;
        }

        EnsureConfigured();

        var key = ExtractObjectKey(fileUrlOrKey);
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1))
        };

        return GetClient().GetPreSignedURL(request);
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client?.Dispose();
        }
    }

    private IAmazonS3 GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        lock (_gate)
        {
            if (_client is not null)
            {
                return _client;
            }

            var config = new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl.TrimEnd('/'),
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            };

            _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
            _ownsClient = true;
            return _client;
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKey)
            || string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.ServiceUrl)
            || string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException(
                "S3 configuration is incomplete. Set S3:AccessKey, S3:SecretKey, S3:ServiceUrl, and S3:BucketName.");
        }
    }

    private string ExtractObjectKey(string fileUrlOrKey)
    {
        var value = fileUrlOrKey.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            return value.TrimStart('/');
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return value;
        }

        var path = uri.AbsolutePath.TrimStart('/');
        var bucketPrefix = _options.BucketName.Trim() + "/";
        if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[bucketPrefix.Length..];
        }

        return path;
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName).Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "document.bin" : name;
    }
}
