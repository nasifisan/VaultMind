using VaultMind.API.Models;

namespace VaultMind.API.Interfaces;

public interface IVectorStoreService
{
    /// <summary>
    /// Embeds and stores document chunks in the vector database.
    /// </summary>
    Task StoreChunksAsync(List<DocumentChunk> chunks, Guid conversationId);

    /// <summary>
    /// Searches for the most relevant document chunks based on a query.
    /// </summary>
    Task<List<RetrievedChunk>> SearchAsync(string query, Guid conversationId, int topK = 5);

    /// <summary>
    /// Removes all chunks associated with a document.
    /// </summary>
    Task DeleteDocumentChunksAsync(Guid documentId, Guid conversationId);
}
