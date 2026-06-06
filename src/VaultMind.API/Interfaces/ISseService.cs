namespace VaultMind.API.Interfaces;

/// <summary>
/// Centralized SSE (Server-Sent Events) service.
/// Any endpoint can use this to stream data to the client
/// without knowing the SSE protocol details.
/// </summary>
public interface ISseService
{
    /// <summary>
    /// Prepares the HTTP response for SSE streaming.
    /// Must be called before sending any events.
    /// </summary>
    void PrepareResponse(HttpContext http);

    /// <summary>
    /// Sends a single SSE data event to the client.
    /// </summary>
    Task SendEventAsync(HttpContext http, string data);

    /// <summary>
    /// Sends a named SSE event (e.g., "event: error\ndata: ...\n\n").
    /// </summary>
    Task SendEventAsync(HttpContext http, string eventName, string data);

    /// <summary>
    /// Sends the [DONE] signal to indicate the stream is complete.
    /// </summary>
    Task SendDoneAsync(HttpContext http);

    /// <summary>
    /// Sends an error event to the client.
    /// </summary>
    Task SendErrorAsync(HttpContext http, string errorMessage);

    /// <summary>
    /// Streams an IAsyncEnumerable of strings as SSE events.
    /// Handles errors and sends [DONE] automatically.
    /// </summary>
    Task StreamAsync(HttpContext http, IAsyncEnumerable<string> tokens);
}
