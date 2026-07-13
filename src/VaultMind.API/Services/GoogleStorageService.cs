using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Memory;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class GoogleStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly ILogger<GoogleStorageService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _bucketName;
    private readonly UrlSigner? _urlSigner;

    public GoogleStorageService(IMemoryCache memoryCache, ILogger<GoogleStorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _cache = memoryCache;
        _bucketName = configuration["GCS:BucketName"] ?? throw new ArgumentException("GCS BucketName is not configured in settings.");

        var credentialsPath = configuration["GCS:CredentialsPath"];
        if (!string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath))
        {
            // Explicitly load credentials from the configured JSON file path using a Stream
#pragma warning disable CS0618 // Type or member is obsolete
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream);
#pragma warning restore CS0618
            _storageClient = StorageClient.Create(credential);

            // Unwrap to ServiceAccountCredential for URL signing
            // GoogleCredential wraps the real credential — we must unwrap it
            var serviceCredential = credential.UnderlyingCredential as ServiceAccountCredential;
            if (serviceCredential == null)
            {
                // Try creating a scoped credential first, then unwrap
                var scoped = credential.CreateScoped();
                serviceCredential = scoped.UnderlyingCredential as ServiceAccountCredential;
            }

            if (serviceCredential != null)
            {
                _urlSigner = UrlSigner.FromCredential(serviceCredential);
            }
            else
            {
                // Last resort: reload from file directly as ServiceAccountCredential
                using var signerStream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
                var sac = ServiceAccountCredential.FromServiceAccountData(signerStream);
                if (sac != null)
                {
                    _urlSigner = UrlSigner.FromCredential(sac);
                }
            }
        }
        else
        {
            // Fall back to environment variable GOOGLE_APPLICATION_CREDENTIALS or GCE/GKE IAM role defaults
            _storageClient = StorageClient.Create();
        }
    }

    public async Task<string> UploadFileAsync(Guid fileId, string fileName, Stream fileStream, string contentType)
    {
        // Construct a unique storage name using the Guid prefix to prevent filename collisions
        var objectName = $"{fileId}_{fileName}";

        var dataObject = await _storageClient.UploadObjectAsync(
            bucket: _bucketName,
            objectName: objectName,
            contentType: contentType,
            source: fileStream
        );

        // Return the canonical GCS URL representation of the uploaded object
        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task<string> GetSignedUrlAsync(string storageUrl, TimeSpan expiry)
    {
        if (_urlSigner == null)
            return storageUrl; // Fallback: return raw URL if no service account key available

        var cacheKey = $"signedurl:{storageUrl}";

        if (!_cache.TryGetValue(cacheKey, out string? signedUrl) || signedUrl == null)
        {
            _logger.LogInformation("Storage Url cache MISS for query");
            var objectName = ExtractObjectName(storageUrl);
            signedUrl = await _urlSigner.SignAsync(_bucketName, objectName, expiry);
            // Cache for slightly less than the expiry to avoid serving expired URLs
            var cacheTtl = expiry > TimeSpan.FromMinutes(5) ? expiry - TimeSpan.FromMinutes(5) : expiry;
            _cache.Set(cacheKey, signedUrl, cacheTtl);
        }
        else
        {
            _logger.LogInformation("Storage Url cache HIT for query");
        }

        return signedUrl;
    }

    public async Task<Stream> DownloadFileAsync(string storageUrl)
    {
        var objectName = ExtractObjectName(storageUrl);
        var memoryStream = new MemoryStream();

        // Use a generous timeout to prevent silent hangs on large files
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _storageClient.DownloadObjectAsync(
            bucket: _bucketName,
            objectName: objectName,
            destination: memoryStream,
            cancellationToken: cts.Token
        );
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Extracts the GCS object name from the full storage URL.
    /// Handles URLs like: https://storage.googleapis.com/bucket-name/object-name
    /// </summary>
    private string ExtractObjectName(string storageUrl)
    {
        string objectName;

        // Find the bucket name in the URL and take everything after it
        var bucketPrefix = $"{_bucketName}/";
        var idx = storageUrl.IndexOf(bucketPrefix, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            objectName = storageUrl.Substring(idx + bucketPrefix.Length);
        }
        else
        {
            // Fallback: try URI-based parsing
            var uri = new Uri(storageUrl);
            var path = uri.AbsolutePath;
            if (path.StartsWith($"/{_bucketName}/", StringComparison.OrdinalIgnoreCase))
            {
                objectName = path.Substring(_bucketName.Length + 2); // skip /<bucket>/
            }
            else
            {
                objectName = storageUrl;
            }
        }

        // URL-decode the object name (e.g. convert %E0%A6... or %20 back to raw characters)
        // so that the GCS SDK can locate the actual file
        return Uri.UnescapeDataString(objectName);
    }
}