const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5139";

/**
 * Streams the chat completion from the backend SSE endpoint.
 * @param message - The user's input message.
 * @param onToken - Callback when a new text token is received.
 * @param onDone - Callback when streaming finishes successfully.
 * @param onError - Callback when an error occurs.
 * @returns The reader, which can be used to cancel streaming.
 */
export async function streamChat(
  message: string,
  onToken: (token: string) => void,
  onDone: () => void,
  onError: (err: Error) => void
): Promise<ReadableStreamDefaultReader<Uint8Array> | undefined> {
  try {
    const response = await fetch(`${API_URL}/api/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ message }),
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    const reader = response.body!.getReader();
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

              const rawData = line.slice(6);
              onToken(rawData);
            }
          }
        }
      } catch (err) {
        onError(err as Error);
      }
    })();

    return reader;
  } catch (err) {
    onError(err as Error);
    throw err;
  }
}

/**
 * Checks the health of the backend API.
 */
export async function checkHealth(): Promise<boolean> {
  try {
    const response = await fetch(`${API_URL}/api/health`, { method: "GET" });
    return response.ok;
  } catch {
    return false;
  }
}
