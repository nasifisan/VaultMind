using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class SseService : ISseService
{
    private readonly ILogger<SseService> _logger;

    public SseService(ILogger<SseService> logger)
    {
        _logger = logger;
    }

    public void PrepareResponse(HttpContext http)
    {
        http.Response.Headers.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Connection = "keep-alive";
    }

    public async Task SendEventAsync(HttpContext http, string data)
    {
        await http.Response.WriteAsync($"data: {data}\n\n");
        await http.Response.Body.FlushAsync();
    }

    public async Task SendEventAsync(HttpContext http, string eventName, string data)
    {
        await http.Response.WriteAsync($"event: {eventName}\ndata: {data}\n\n");
        await http.Response.Body.FlushAsync();
    }

    public async Task SendDoneAsync(HttpContext http)
    {
        await http.Response.WriteAsync("data: [DONE]\n\n");
        await http.Response.Body.FlushAsync();
    }

    public async Task SendErrorAsync(HttpContext http, string errorMessage)
    {
        _logger.LogError("SSE stream error: {Error}", errorMessage);
        await http.Response.WriteAsync($"event: error\ndata: {errorMessage}\n\n");
        await http.Response.Body.FlushAsync();
    }

    public async Task StreamAsync(HttpContext http, IAsyncEnumerable<string> tokens)
    {
        PrepareResponse(http);

        try
        {
            await foreach (var token in tokens)
            {
                if (token is not null)
                {
                    await SendEventAsync(http, token);
                }
            }
            await SendDoneAsync(http);
        }
        catch (Exception ex)
        {
            await SendErrorAsync(http, ex.Message);
        }
    }
}
