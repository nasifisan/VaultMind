using VaultMind.API.Models;

namespace VaultMind.API.Interfaces;

public interface IIngestionService
{
    /// <summary>
    /// Processes an uploaded document record (Download → Extract text → Chunk → Embed → Save in Qdrant).
    /// </summary>
    /// <param name="record">The metadata record of the document.</param>
    Task ProcessDocumentAsync(DocumentRecord record);
}
