using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class GoogleStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly UrlSigner? _urlSigner;

    public GoogleStorageService(IConfiguration configuration)
    {
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
            var serviceCredential = credential.UnderlyingCredential as ServiceAccountCredential;
            if (serviceCredential != null)
            {
                _urlSigner = UrlSigner.FromCredential(serviceCredential);
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

        var objectName = storageUrl.Split($"{_bucketName}/")[1];
        var signedUrl = await _urlSigner.SignAsync(_bucketName, objectName, expiry);
        return signedUrl;
    }
}