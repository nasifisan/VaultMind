const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5139";

/**
 * Streams the chat completion from the backend SSE endpoint.
 * @param {string} message - The user's input message.
 * @param {function(string)} onToken - Callback when a new text token is received.
 * @param {function()} onDone - Callback when streaming finishes successfully.
 * @param {function(Error)} onError - Callback when an error occurs.
 * @returns {Promise<ReadableStreamReader>} - The reader, which can be used to cancel streaming.
 */
export async function streamChat(message, onToken, onDone, onError) {
  try {
    const response = await fetch(`${API_URL}/api/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message }),
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    // Run async reading loop
    (async () => {
      try {
        let buffer = "";
        while (true) {
          const { done, value } = await reader.read();
          if (done) {
            onDone();
            break;
          }

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split("\n");
          // Keep the last partial line in the buffer
          buffer = lines.pop() || "";

          for (const line of lines) {
            if (line.startsWith("data: ")) {
              const data = line.slice(6).trim();

              if (data === "[DONE]") {
                onDone();
                return;
              }
              if (data.startsWith("[ERROR]")) {
                throw new Error(data.slice(8));
              }

              // Normal token
              // We want to pass the data raw (sometimes spaces/newlines matter, but C# SSE is formatted line-by-line)
              // Wait, in standard page.js: const data = line.slice(6); (not trimmed)
              // Let's do: line.slice(6) to preserve leading/trailing spaces for proper formatting
              const rawData = line.slice(6);
              onToken(rawData);
            }
          }
        }
      } catch (err) {
        onError(err);
      }
    })();

    return reader;
  } catch (err) {
    onError(err);
    throw err;
  }
}

/**
 * Checks the health of the backend API.
 * @returns {Promise<boolean>}
 */
export async function checkHealth() {
  try {
    const response = await fetch(`${API_URL}/api/health`, { method: "GET" });
    return response.ok;
  } catch (error) {
    return false;
  }
}
