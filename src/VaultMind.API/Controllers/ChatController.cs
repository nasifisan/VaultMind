using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ISseService _sseService;
    private readonly IMongoRepository<Conversation> _conversationsRepo;

    public ChatController(
        Kernel kernel,
        ISseService sseService,
        IMongoRepository<Conversation> conversationsRepo)
    {
        _kernel = kernel;
        _sseService = sseService;
        _conversationsRepo = conversationsRepo;
    }

    [HttpPost]
    public async Task Post([FromBody] ChatRequest request)
    {
        var userId = GetCurrentUserId();
        var tokens = GetChatTokensAsync(request.ConversationId, request.Content, userId);
        await _sseService.StreamAsync(HttpContext, tokens);
    }

    private async IAsyncEnumerable<string> GetChatTokensAsync(Guid conversationId, string userMessage, Guid userId)
    {
        // 1. Retrieve the conversation from MongoDB
        var conversation = await _conversationsRepo.GetByIdAsync(conversationId);

        if (conversation == null)
        {
            // First message in a new conversation: initialize it
            conversation = new Conversation
            {
                Id = conversationId,
                UserId = userId,
                Title = userMessage.Length > 40 ? userMessage.Substring(0, 40) + "..." : userMessage,
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
                throw new UnauthorizedAccessException("You do not have permission to access this conversation.");
            }
        }

        // 2. Append the new user message to the conversation history
        conversation.Messages.Add(new ConversationMessage
        {
            Role = ConversationRoles.User,
            Content = userMessage,
            Timestamp = DateTime.UtcNow
        });

        // 3. Format the preceding conversation history for the prompt context
        string formattedHistory = "";
        if (conversation.Messages.Count > 1)
        {
            var historyLines = new List<string>();
            // Build history using all messages prior to the latest message
            for (int i = 0; i < conversation.Messages.Count - 1; i++)
            {
                var msg = conversation.Messages[i];
                string sender = string.Equals(msg.Role, ConversationRoles.User, StringComparison.OrdinalIgnoreCase) ? ConversationRoles.User : ConversationRoles.Assistant;
                historyLines.Add($"{sender}: {msg.Content}");
            }
            formattedHistory = string.Join("\n", historyLines);
        }

        // 4. Configure Semantic Kernel execution arguments (settings are loaded from config.json)
        var arguments = new KernelArguments
        {
            { "input", userMessage },
            { "history", formattedHistory },
            { "style", "professional, helpful, and concise" }
        };

        var chatFunction = _kernel.Plugins["ChatPlugin"]["VaultMindChat"];
        var responseStream = _kernel.InvokeStreamingAsync<string>(chatFunction, arguments);

        var fullResponseBuilder = new StringBuilder();

        // 5. Yield back each token chunk as it is streamed from local LLM
        await foreach (var chunk in responseStream)
        {
            if (chunk is not null)
            {
                fullResponseBuilder.Append(chunk);
                yield return chunk;
            }
        }

        // 6. Append the full assistant response to the conversation history and persist to database
        var assistantReply = fullResponseBuilder.ToString();
        if (!string.IsNullOrEmpty(assistantReply))
        {
            conversation.Messages.Add(new ConversationMessage
            {
                Role = ConversationRoles.Assistant,
                Content = assistantReply,
                Timestamp = DateTime.UtcNow
            });

            // Update title if it was initialized as default "New Chat"
            if (conversation.Title == "New Chat" && conversation.Messages.Count > 0)
            {
                var firstUserMessage = conversation.Messages.FirstOrDefault(m => m.Role == ConversationRoles.User)?.Content;
                if (!string.IsNullOrEmpty(firstUserMessage))
                {
                    conversation.Title = firstUserMessage.Length > 40 ? firstUserMessage.Substring(0, 40) + "..." : firstUserMessage;
                }
            }

            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationsRepo.ReplaceOneAsync(conversation);
        }
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

public record ChatRequest(Guid ConversationId, string Content);
