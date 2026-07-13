using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMongoRepository<Conversation> _conversationsRepo;

    public ConversationsController(IMongoRepository<Conversation> conversationsRepo)
    {
        _conversationsRepo = conversationsRepo;
    }

    // GET: api/conversations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConversationHeaderDto>>> GetConversations()
    {
        var userId = GetCurrentUserId();

        // Find conversations matching the current user ID
        var list = await _conversationsRepo.FindAsync(c => c.UserId == userId);

        var headers = list
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationHeaderDto(c.Id, c.Title, c.CreatedAt, c.UpdatedAt));

        return Ok(headers);
    }

    // GET: api/conversations/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Conversation>> GetConversation(Guid id)
    {
        var userId = GetCurrentUserId();
        var conversation = await _conversationsRepo.GetByIdAsync(id);

        if (conversation == null)
        {
            return NotFound(new { Error = "Conversation not found" });
        }

        // Validate ownership
        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        return Ok(conversation);
    }

    // POST: api/conversations
    [HttpPost]
    public async Task<ActionResult<Conversation>> CreateOrUpdateConversation([FromBody] SaveConversationRequest request)
    {
        var userId = GetCurrentUserId();

        var id = request.Id ?? Guid.NewGuid();
        var conversation = await _conversationsRepo.GetByIdAsync(id);

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = id,
                UserId = userId,
                Title = request.Title ?? "New Chat",
                Messages = new List<ConversationMessage>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _conversationsRepo.InsertOneAsync(conversation);
        }
        else
        {
            // Validate ownership
            if (conversation.UserId != userId)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                conversation.Title = request.Title;
            }
            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationsRepo.ReplaceOneAsync(conversation);
        }

        return Ok(conversation);
    }

    // PUT: api/conversations/{id}/title
    [HttpPut("{id}/title")]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateTitleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { Error = "Title cannot be empty" });
        }

        var userId = GetCurrentUserId();
        var conversation = await _conversationsRepo.GetByIdAsync(id);

        if (conversation == null)
        {
            return NotFound(new { Error = "Conversation not found" });
        }

        // Validate ownership
        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        conversation.Title = request.Title;
        conversation.UpdatedAt = DateTime.UtcNow;

        await _conversationsRepo.ReplaceOneAsync(conversation);

        return NoContent();
    }

    // DELETE: api/conversations/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var userId = GetCurrentUserId();
        var conversation = await _conversationsRepo.GetByIdAsync(id);

        if (conversation == null)
        {
            return NotFound(new { Error = "Conversation not found" });
        }

        // Validate ownership
        if (conversation.UserId != userId)
        {
            return Forbid();
        }

        await _conversationsRepo.DeleteByIdAsync(id);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return Guid.Empty; // Anonymous guest fall-back
    }
}

// ── DTOs ──
public record ConversationHeaderDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);
public record SaveConversationRequest(Guid? Id, string? Title);
public record UpdateTitleRequest(string Title);
