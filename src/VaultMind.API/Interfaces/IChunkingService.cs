using VaultMind.API.Models;

namespace VaultMind.API.Interfaces;

public interface IChunkingService
{
    /// <summary>
    /// Splits the extracted text into overlapping chunks.
    /// </summary>
    /// <param name="text">The full extracted text.</param>
    /// <param name="documentId">The ID of the document.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>A list of document chunks.</returns>
    List<DocumentChunk> ChunkText(string text, Guid documentId, string fileName);
}
