namespace VaultMind.API.Interfaces;

public interface IDocumentParserService
{
    /// <summary>
    /// Extracts plain text from the given file stream based on the content type.
    /// </summary>
    /// <param name="fileStream">The readable stream of the file.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="fileName">The name of the file (can be used as fallback or for extensions).</param>
    /// <returns>Extracted plain text from the file.</returns>
    Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName);

    /// <summary>
    /// Determines whether the service can parse files of the given content type.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>True if supported, false otherwise.</returns>
    bool CanParse(string contentType);
}
