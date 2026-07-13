import { generateGuid } from "@/shared/utils";
import { apiFetch } from "./apiClient";
import {
  Conversation,
  ConversationHeader,
} from "@/types/conversation/conversation.contracts";
import { DocumentRecord } from "@/types/document/document.contracts";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5152";

/**
 * Streams the chat completion from the backend SSE endpoint.
 * @param conversationId - The unique Guid of the conversation.
 * @param content - The latest user message text.
 * @param onToken - Callback when a new text token is received.
 * @param onDone - Callback when streaming finishes successfully.
 * @param onError - Callback when an error occurs.
 * @returns The reader, which can be used to cancel streaming.
 */
export async function streamChat(
  conversationId: string,
  content: string,
  onToken: (token: string) => void,
  onDone: () => void,
  onError: (err: Error) => void,
): Promise<ReadableStreamDefaultReader<Uint8Array> | undefined> {
  try {
    const response = await apiFetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ConversationId: conversationId,
        Content: content,
      }),
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
 * Fetch all conversation headers belonging to the current session/user.
 */
export async function getConversations(): Promise<ConversationHeader[]> {
  const response = await apiFetch("/api/conversations", {
    method: "GET",
  });
  if (!response.ok) {
    throw new Error(`Failed to load conversations: ${response.status}`);
  }
  return response.json() as Promise<ConversationHeader[]>;
}

/**
 * Retrieve the full conversation detailing its message history.
 */
export async function getConversation(id: string): Promise<Conversation> {
  const response = await apiFetch(`/api/conversations/${id}`, {
    method: "GET",
  });
  if (!response.ok) {
    throw new Error(`Failed to load conversation details: ${response.status}`);
  }
  return response.json() as Promise<Conversation>;
}

/**
 * Explicitly save/create a new conversation or update its metadata.
 */
export async function saveConversation(
  id: string,
  title?: string,
): Promise<Conversation> {
  const response = await apiFetch("/api/conversations", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      Id: id,
      Title: title || null,
    }),
  });
  if (!response.ok) {
    throw new Error(`Failed to save conversation: ${response.status}`);
  }
  return response.json() as Promise<Conversation>;
}

/**
 * Update the title of a specific conversation manually.
 */
export async function updateConversationTitle(
  id: string,
  title: string,
): Promise<void> {
  const response = await apiFetch(`/api/conversations/${id}/title`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ Title: title }),
  });
  if (!response.ok) {
    throw new Error(`Failed to update title: ${response.status}`);
  }
}

/**
 * Delete a conversation.
 */
export async function deleteConversation(id: string): Promise<void> {
  const response = await apiFetch(`/api/conversations/${id}`, {
    method: "DELETE",
  });
  if (!response.ok) {
    throw new Error(`Failed to delete conversation: ${response.status}`);
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

/**
 * Upload a document attached to a conversation.
 */
export async function uploadDocument(
  conversationId: string,
  file: File,
): Promise<DocumentRecord> {
  const formData = new FormData();
  formData.append("id", generateGuid());
  formData.append("conversationId", conversationId);
  formData.append("file", file);

  const response = await apiFetch("/api/documents/upload", {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(`Failed to upload document: ${response.status}`);
  }

  return response.json() as Promise<DocumentRecord>;
}

/**
 * Fetch all documents associated with a specific conversation.
 */
export async function getConversationDocuments(
  conversationId: string,
): Promise<DocumentRecord[]> {
  const response = await apiFetch(
    `/api/documents/conversation/${conversationId}`,
    {
      method: "GET",
    },
  );

  if (!response.ok) {
    throw new Error(
      `Failed to load documents for conversation: ${response.status}`,
    );
  }

  return response.json() as Promise<DocumentRecord[]>;
}

/**
 * Delete a document by its unique ID.
 */
export async function deleteDocument(id: string): Promise<void> {
  const response = await apiFetch(`/api/documents/${id}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    throw new Error(`Failed to delete document: ${response.status}`);
  }
}
