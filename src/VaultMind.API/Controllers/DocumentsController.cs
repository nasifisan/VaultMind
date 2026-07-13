using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IMongoRepository<DocumentRecord> _documentsRepo;
    private readonly IIngestionService _ingestionService;
    private readonly IVectorStoreService _vectorStoreService;

    public DocumentsController(
        IStorageService storageService,
        IMongoRepository<DocumentRecord> documentsRepo,
        IIngestionService ingestionService,
        IVectorStoreService vectorStoreService)
    {
        _storageService = storageService;
        _documentsRepo = documentsRepo;
        _ingestionService = ingestionService;
        _vectorStoreService = vectorStoreService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] Guid id, [FromForm] Guid conversationId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "No file was uploaded." });
        }

        if (id == Guid.Empty)
        {
            return BadRequest(new { Error = "A valid document ID (Guid) must be provided." });
        }

        if (conversationId == Guid.Empty)
        {
            return BadRequest(new { Error = "A valid conversation ID (Guid) must be provided." });
        }

        var userId = GetCurrentUserId();

        try
        {
            // Compute SHA-256 hash of the file stream to detect duplicates
            string contentHash;
            using (var stream = file.OpenReadStream())
            {
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    contentHash = Convert.ToHexString(hashBytes);
                }
            }

            // Check if document with same ContentHash + ConversationId exists in this conversation
            var existing = await _documentsRepo.FindOneAsync(d => d.ContentHash == contentHash && d.ConversationId == conversationId);
            if (existing != null)
            {
                return Conflict(new { Error = "This file has already been uploaded to this conversation." });
            }

            string storageUrl;
            using (var stream = file.OpenReadStream())
            {
                storageUrl = await _storageService.UploadFileAsync(id, file.FileName, stream, file.ContentType);
            }

            // Create a new document metadata record to persist in MongoDB
            var documentRecord = new DocumentRecord
            {
                Id = id,
                UserId = userId,
                ConversationId = conversationId,
                FileName = file.FileName,
                StorageUrl = storageUrl,
                ContentType = file.ContentType,
                Size = file.Length,
                ContentHash = contentHash,
                UploadedAt = DateTime.UtcNow
            };

            await _documentsRepo.InsertOneAsync(documentRecord);

            // Trigger fire-and-forget background processing
            _ = Task.Run(async () =>
            {
                try
                {
                    await _ingestionService.ProcessDocumentAsync(documentRecord);
                }
                catch
                {
                    // Swallowed: IngestionService has its own internal logging
                }
            });

            return CreatedAtAction(nameof(GetDocumentById), new { id = documentRecord.Id }, documentRecord);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"Failed to upload file to storage: {ex.Message}" });
        }
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<ActionResult<List<DocumentRecord>>> GetDocumentsByConversation(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var records = await _documentsRepo.FindAsync(d => d.ConversationId == conversationId && d.UserId == userId);
        // Replace raw GCS URLs with time-limited signed URLs
        foreach (var record in records)
        {
            record.StorageUrl = await _storageService.GetSignedUrlAsync(record.StorageUrl, TimeSpan.FromHours(1));
        }
        return Ok(records);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentRecord>> GetDocumentById(Guid id)
    {
        var userId = GetCurrentUserId();
        var record = await _documentsRepo.GetByIdAsync(id);

        if (record == null)
        {
            return NotFound(new { Error = "Document record not found." });
        }

        // Validate ownership of the document
        if (record.UserId != userId)
        {
            return Forbid();
        }

        record.StorageUrl = await _storageService.GetSignedUrlAsync(record.StorageUrl, TimeSpan.FromHours(1));

        return Ok(record);
    }

    [HttpGet("{id}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(Guid id)
    {
        var userId = GetCurrentUserId();
        var record = await _documentsRepo.GetByIdAsync(id);

        if (record == null)
        {
            return NotFound(new { Error = "Document record not found." });
        }

        if (record.UserId != userId)
        {
            return Forbid();
        }

        var signedUrl = await _storageService.GetSignedUrlAsync(record.StorageUrl, TimeSpan.FromMinutes(15));
        return Ok(new { url = signedUrl });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var userId = GetCurrentUserId();
        var record = await _documentsRepo.GetByIdAsync(id);

        if (record == null)
        {
            return NotFound(new { Error = "Document record not found." });
        }

        if (record.UserId != userId)
        {
            return Forbid();
        }

        await _vectorStoreService.DeleteDocumentChunksAsync(id, record.ConversationId);
        await _documentsRepo.DeleteByIdAsync(id);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}
