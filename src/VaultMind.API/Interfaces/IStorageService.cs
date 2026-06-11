using System;
using System.IO;
using System.Threading.Tasks;

namespace VaultMind.API.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file stream to Google Cloud Storage and returns its access URL.
    /// </summary>
    /// <param name="fileId">The unique ID assigned to this document.</param>
    /// <param name="fileName">The original name of the file.</param>
    /// <param name="fileStream">The readable stream of the file content.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <returns>The storage access URL of the uploaded object.</returns>
    Task<string> UploadFileAsync(Guid fileId, string fileName, Stream fileStream, string contentType);
}
