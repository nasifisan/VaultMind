using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class GoogleStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

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

        // Return the canonical GCS public/authenticated URL representation of the uploaded object
        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }
}
