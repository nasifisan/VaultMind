using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IConfiguration _configuration;

    public ChatController(
        Kernel kernel,
        ISseService sseService,
        IMongoRepository<Conversation> conversationsRepo,
        IVectorStoreService vectorStoreService,
        IConfiguration configuration)
    {
        _kernel = kernel;
        _sseService = sseService;
        _conversationsRepo = conversationsRepo;
        _vectorStoreService = vectorStoreService;
        _configuration = configuration;
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

        // 2. Load system instructions from skprompt.txt
        string systemPrompt = "Your name is VaultMind. You are an intelligent document analysis assistant.";
        try
        {
            var promptsPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ChatPlugin", "VaultMindChat", "skprompt.txt");
            if (System.IO.File.Exists(promptsPath))
            {
                systemPrompt = await System.IO.File.ReadAllTextAsync(promptsPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to load skprompt.txt: {ex.Message}");
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        // Read configuration settings for history window and RAG context size
        var historyWindowSize = _configuration.GetValue<int>("Chat:HistoryWindowSize", 6);
        var maxRetrievedChunks = _configuration.GetValue<int>("Chat:MaxRetrievedChunks", 5);

        // Add prior history from MongoDB database
        var recentMessages = conversation.Messages.TakeLast(historyWindowSize).ToList();
        foreach (var msg in recentMessages)
        {
            if (string.Equals(msg.Role, ConversationRoles.User, StringComparison.OrdinalIgnoreCase))
            {
                chatHistory.AddUserMessage(msg.Content);
            }
            else
            {
                chatHistory.AddAssistantMessage(msg.Content);
            }
        }

        // Perform RAG retrieval to fetch relevant document context
        try
        {
            var retrievedChunks = await _vectorStoreService.SearchAsync(userMessage, conversationId, topK: maxRetrievedChunks);
            if (retrievedChunks != null && retrievedChunks.Count > 0)
            {
                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("Use the following context from the user's uploaded documents to answer their question. Be factual, grounded, and reference the source filenames when answering.");
                foreach (var chunk in retrievedChunks)
                {
                    contextBuilder.AppendLine($"\n--- START CONTEXT ---");
                    contextBuilder.AppendLine($"Source Document: {chunk.FileName}");
                    contextBuilder.AppendLine($"Content:\n{chunk.Content}");
                    contextBuilder.AppendLine($"--- END CONTEXT ---");
                }
                chatHistory.AddSystemMessage(contextBuilder.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to retrieve document context: {ex.Message}");
        }

        // 3. Append the new user message to the conversation history database object
        conversation.Messages.Add(new ConversationMessage
        {
            Role = ConversationRoles.User,
            Content = userMessage,
            Timestamp = DateTime.UtcNow
        });

        // Add the latest user message to the active ChatHistory
        chatHistory.AddUserMessage(userMessage);

        // 4. Configure execution settings
        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.7,
            MaxTokens = 1000,
            StopSequences = new List<string> { "User:", "Assistant:", "<|user|>", "<|assistant|>", "<|end|>" },
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        // --- PRINT THE STRUCTURED CHAT MESSAGES ---
        Console.WriteLine("\n--- CHAT MESSAGES SENT TO LLM ---");
        foreach (var message in chatHistory)
        {
            Console.WriteLine($"[{message.Role}]: {message.Content}");
        }
        Console.WriteLine("---------------------------------\n");

        // 5. Get Streaming completions using IChatCompletionService
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var responseStream = chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory, settings, _kernel);

        var fullResponseBuilder = new StringBuilder();

        // Yield back each token chunk as it is streamed from local LLM
        await foreach (var chunk in responseStream)
        {
            if (chunk?.Content is not null)
            {
                fullResponseBuilder.Append(chunk.Content);
                yield return chunk.Content;
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
