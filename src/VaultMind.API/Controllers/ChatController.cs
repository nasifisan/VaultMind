using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ISseService _sseService;

    public ChatController(Kernel kernel, ISseService sseService)
    {
        _kernel = kernel;
        _sseService = sseService;
    }

    [HttpPost]
    public async Task Post([FromBody] ChatRequest request)
    {
        var tokens = GetChatTokensAsync(request.Messages);
        await _sseService.StreamAsync(HttpContext, tokens);
    }

    private async IAsyncEnumerable<string> GetChatTokensAsync(List<ChatMessageDto> messages)
    {
        string latestInput = "";
        string formattedHistory = "";

        if (messages != null && messages.Count > 0)
        {
            latestInput = messages.Last().Content;

            if (messages.Count > 1)
            {
                var historyLines = new List<string>();
                for (int i = 0; i < messages.Count - 1; i++)
                {
                    var msg = messages[i];
                    string sender = string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase) ? "User" : "Assistant";
                    historyLines.Add($"{sender}: {msg.Content}");
                }
                formattedHistory = string.Join("\n", historyLines);
            }
        }

        var settings = new OpenAIPromptExecutionSettings();

        var arguments = new KernelArguments(settings)
        {
            { "input", latestInput },
            { "history", formattedHistory },
            { "style", "professional, helpful, and concise" }
        };

        var chatFunction = _kernel.Plugins["ChatPlugin"]["VaultMindChat"];

        // Read and render the prompt template from disk for debugging
        //var templatePath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ChatPlugin", "VaultMindChat", "skprompt.txt");
        //if (System.IO.File.Exists(templatePath))
        //{
        //    try
        //    {
        //        string templateString = await System.IO.File.ReadAllTextAsync(templatePath);
        //        var promptConfig = new PromptTemplateConfig(templateString);
        //        var templateFactory = new KernelPromptTemplateFactory();
        //        var promptTemplate = templateFactory.Create(promptConfig);

        //        string renderedPrompt = await promptTemplate.RenderAsync(_kernel, arguments);
        //        Console.WriteLine("\n=== [DEBUG] RENDERED PROMPT SENT TO LLM ===");
        //        Console.WriteLine(renderedPrompt);
        //        Console.WriteLine("============================================\n");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[DEBUG] Failed to render prompt: {ex.Message}");
        //    }
        //}

        var responseStream = _kernel.InvokeStreamingAsync<string>(chatFunction, arguments);

        await foreach (var chunk in responseStream)
        {
            if (chunk is not null)
            {
                yield return chunk;
            }
        }
    }
}

public record ChatMessageDto(string Role, string Content);
public record ChatRequest(List<ChatMessageDto> Messages);
