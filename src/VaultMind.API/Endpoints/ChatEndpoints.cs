using Microsoft.SemanticKernel.ChatCompletion;
using VaultMind.API.Services;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Endpoints;

public static class ChatEndpoints
{
    public static WebApplication MapChatEndpoints(this WebApplication app)
    {
        app.MapPost("/api/chat", HandleChatStream);

        return app;
    }

    private static async Task HandleChatStream(
        ChatRequest request,
        HttpContext http,
        IChatCompletionService chatService,
        ISseService sseService)
    {
        var tokens = GetChatTokensAsync(chatService, request.Message);
        await sseService.StreamAsync(http, tokens);
    }

    private static async IAsyncEnumerable<string> GetChatTokensAsync(
        IChatCompletionService chatService,
        string message)
    {
        // Build chat history with system prompt
        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are VaultMind, an intelligent document analysis assistant. " +
            "You are helpful, concise, and knowledgeable. " +
            "When you don't know something, you say so honestly."
        );
        history.AddUserMessage(message);

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(history))
        {
            if (chunk.Content is not null)
            {
                yield return chunk.Content;
            }
        }
    }
}

// ── Request Models ──
public record ChatRequest(string Message);
