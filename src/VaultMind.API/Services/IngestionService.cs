using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Services;

public class IngestionService : IIngestionService
{
    private readonly IStorageService _storageService;
    private readonly IDocumentParserService _parserService;
    private readonly IChunkingService _chunkingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IMongoRepository<DocumentRecord> _documentRepository;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IStorageService storageService,
        IDocumentParserService parserService,
        IChunkingService chunkingService,
        IVectorStoreService vectorStoreService,
        IMongoRepository<DocumentRecord> documentRepository,
        ILogger<IngestionService> logger)
    {
        _storageService = storageService;
        _parserService = parserService;
        _chunkingService = chunkingService;
        _vectorStoreService = vectorStoreService;
        _documentRepository = documentRepository;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(DocumentRecord record)
    {
        _logger.LogInformation("Starting ingestion pipeline for document {FileName} ({DocumentId})", record.FileName, record.Id);

        try
        {
            // 1. Update status in MongoDB to Processing
            record.ProcessingStatus = "Processing";
            await _documentRepository.ReplaceOneAsync(record);

            // 2. Download the file from GCS
            _logger.LogInformation("Downloading file from storage: {StorageUrl}", record.StorageUrl);
            using var fileStream = await _storageService.DownloadFileAsync(record.StorageUrl);

            // 3. Extract text from the file
            _logger.LogInformation("Extracting text from file...");
            var extractedText = await _parserService.ExtractTextAsync(fileStream, record.ContentType, record.FileName);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("No text extracted from document {FileName} ({DocumentId}). It may be scanned or empty.", record.FileName, record.Id);
                record.ProcessingStatus = "Completed";
                await _documentRepository.ReplaceOneAsync(record);
                return;
            }

            // 4. Split text into chunks
            _logger.LogInformation("Chunking extracted text...");
            var chunks = _chunkingService.ChunkText(extractedText, record.Id, record.FileName);

            if (chunks.Count > 0)
            {
                // 5. Generate embeddings and store in Qdrant
                _logger.LogInformation("Generating embeddings and upserting {Count} chunks to Qdrant...", chunks.Count);
                await _vectorStoreService.StoreChunksAsync(chunks, record.ConversationId);
            }
            else
            {
                _logger.LogWarning("No chunks created for document {FileName} ({DocumentId})", record.FileName, record.Id);
            }

            // 6. Update status to Completed
            record.ProcessingStatus = "Completed";
            await _documentRepository.ReplaceOneAsync(record);
            _logger.LogInformation("Ingestion pipeline successfully completed for document {FileName} ({DocumentId})", record.FileName, record.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during ingestion pipeline for document {FileName} ({DocumentId})", record.FileName, record.Id);

            try
            {
                // Set status to Failed
                record.ProcessingStatus = "Failed";
                await _documentRepository.ReplaceOneAsync(record);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update document status to Failed in MongoDB for document {DocumentId}", record.Id);
            }
        }
    }
}
