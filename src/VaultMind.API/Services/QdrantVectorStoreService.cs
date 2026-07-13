using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Services;

public class QdrantVectorStoreService : IVectorStoreService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly QdrantClient _qdrantClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<QdrantVectorStoreService> _logger;
    private readonly string _collectionPrefix;

    public QdrantVectorStoreService(
        ITextEmbeddingGenerationService embeddingService,
        IConfiguration configuration,
        IMemoryCache mermoryCache,
        ILogger<QdrantVectorStoreService> logger)
    {
        _embeddingService = embeddingService;
        _cache = mermoryCache;
        _logger = logger;

        var qdrantEndpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6333";
        _collectionPrefix = (configuration["Qdrant:CollectionName"] ?? "vaultmind").ToLowerInvariant();

        // Extract host from the configured endpoint URI
        var uri = new Uri(qdrantEndpoint);
        var host = uri.Host;
        var grpcPort = 6334; // standard Qdrant gRPC port

        // Create QdrantClient with extended timeout for embedding-heavy workloads
        var grpcAddress = $"http://{host}:{grpcPort}";
        var channel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
                ConnectTimeout = TimeSpan.FromMinutes(5)
            }
        });
        var grpcClient = new QdrantGrpcClient(channel);
        _qdrantClient = new QdrantClient(grpcClient);
        _logger.LogInformation("QdrantVectorStoreService initialized connecting to Qdrant gRPC at {Address} with prefix '{Prefix}'", grpcAddress, _collectionPrefix);
    }

    public async Task StoreChunksAsync(List<DocumentChunk> chunks, Guid conversationId)
    {
        if (chunks == null || chunks.Count == 0)
        {
            return;
        }

        var collectionName = GetCollectionName(conversationId);

        try
        {
            // Ensure the collection exists in Qdrant
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                _logger.LogInformation("Creating Qdrant collection: {CollectionName}", collectionName);
                // default nomic-embed-text size is 768
                await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = 768,
                    Distance = Distance.Cosine
                });
            }

            var points = new List<PointStruct>();
            foreach (var chunk in chunks)
            {
                // Generate vector embedding for chunk content
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);

                var point = new PointStruct
                {
                    Id = chunk.Id,
                    Vectors = embedding.ToArray(),
                    Payload =
                    {
                        ["documentId"] = chunk.DocumentId.ToString(),
                        ["fileName"] = chunk.FileName,
                        ["chunkIndex"] = chunk.ChunkIndex,
                        ["content"] = chunk.Content
                    }
                };
                points.Add(point);
            }

            await _qdrantClient.UpsertAsync(collectionName, points);
            _logger.LogInformation("Successfully stored {Count} chunks in Qdrant collection {CollectionName}", chunks.Count, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store chunks in Qdrant for collection {CollectionName}", collectionName);
            throw;
        }
    }

    public async Task<List<RetrievedChunk>> SearchAsync(string query, Guid conversationId, int topK = 5)
    {
        var collectionName = GetCollectionName(conversationId);
        var results = new List<RetrievedChunk>();

        try
        {
            // If the collection doesn't exist yet, there are no documents in this conversation
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                _logger.LogInformation("Qdrant collection {CollectionName} does not exist. Returning empty search results.", collectionName);
                return results;
            }

            // Generate vector embedding for the query (using cache to avoid redundant LLM/Ollama calls)
            var cacheKey = $"emb:{query.GetHashCode()}";
            if (!_cache.TryGetValue(cacheKey, out ReadOnlyMemory<float> queryEmbedding))
            {
                _logger.LogInformation("Embedding cache MISS for query");
                queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
                _cache.Set(cacheKey, queryEmbedding, TimeSpan.FromMinutes(30));
            }
            else
            {
                _logger.LogInformation("Embedding cache HIT for query");
            }

            // Execute similarity search
            var searchResult = await _qdrantClient.SearchAsync(
                collectionName: collectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)topK
            );

            foreach (var hit in searchResult)
            {
                var score = hit.Score;
                var payload = hit.Payload;

                var content = payload.TryGetValue("content", out var cVal) ? cVal.StringValue : string.Empty;
                var fileName = payload.TryGetValue("fileName", out var fnVal) ? fnVal.StringValue : string.Empty;
                int chunkIndex = payload.TryGetValue("chunkIndex", out var ciVal) ? (int)ciVal.IntegerValue : 0;

                results.Add(new RetrievedChunk
                {
                    Content = content,
                    FileName = fileName,
                    ChunkIndex = chunkIndex,
                    Score = score
                });
            }

            _logger.LogInformation("Found {Count} relevant chunks in Qdrant collection {CollectionName}", results.Count, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Qdrant similarity search in collection {CollectionName}", collectionName);
        }

        return results;
    }

    public async Task DeleteDocumentChunksAsync(Guid documentId, Guid conversationId)
    {
        var collectionName = GetCollectionName(conversationId);

        try
        {
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                return;
            }

            // Delete points that match the documentId filter
            await _qdrantClient.DeleteAsync(collectionName, filter: new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "documentId",
                            Match = new Match { Keyword = documentId.ToString() }
                        }
                    }
                }
            });

            _logger.LogInformation("Successfully deleted chunks for document {DocumentId} from Qdrant collection {CollectionName}", documentId, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunks for document {DocumentId} from Qdrant collection {CollectionName}", documentId, collectionName);
        }
    }

    private string GetCollectionName(Guid conversationId)
    {
        // Prepend prefix to Guid (lowercase alphanumeric or hyphens)
        return $"{_collectionPrefix}-conversation-{conversationId}".ToLowerInvariant();
    }
}
