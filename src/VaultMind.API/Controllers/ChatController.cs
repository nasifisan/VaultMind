using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatCompletionService _chatService;
    private readonly ISseService _sseService;

    public ChatController(IChatCompletionService chatService, ISseService sseService)
    {
        _chatService = chatService;
        _sseService = sseService;
    }

    [HttpPost]
    public async Task Post([FromBody] ChatRequest request)
    {
        var tokens = GetChatTokensAsync(request.Message);
        await _sseService.StreamAsync(HttpContext, tokens);
    }

    private async IAsyncEnumerable<string> GetChatTokensAsync(string message)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(
            "Your name is VaultMind. You are an intelligent document analysis assistant " +
            "built by the VaultMind team. You must ALWAYS identify yourself as VaultMind. " +
            "You are NOT Phi, you are NOT a Microsoft product, you are NOT an OpenAI product. " +
            "Never mention Phi, Microsoft, OpenAI, or any other AI company when asked about yourself. " +
            "If asked 'who are you?' or 'what are you?', respond with: " +
            "'I am VaultMind, an intelligent document analysis assistant.' " +
            "You are helpful, concise, and knowledgeable. " +
            "When you don't know something, you say so honestly."
        );
        history.AddUserMessage(message);

        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(history))
        {
            if (chunk.Content is not null)
            {
                yield return chunk.Content;
            }
        }
    }
}

public record ChatRequest(string Message);
