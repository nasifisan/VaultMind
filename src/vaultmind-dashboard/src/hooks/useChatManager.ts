"use client";

import { useState, useEffect, useRef } from "react";
import { streamChat, checkHealth } from "../services/chatService.service";
import type { Chat, ChatManager } from "../types";

export default function useChatManager(): ChatManager {
  const [chats, setChats] = useState<Chat[]>([]);
  const [activeChatId, setActiveChatId] = useState<string | null>(null);
  const [isStreaming, setIsStreaming] = useState<boolean>(false);
  const [input, setInput] = useState<string>("");
  const [isOnline, setIsOnline] = useState<boolean>(true);
  const [isLoaded, setIsLoaded] = useState<boolean>(false);

  // Keep a ref to the activeChatId so callbacks in streamChat always have the latest value
  const activeChatIdRef = useRef<string | null>(null);
  useEffect(() => {
    activeChatIdRef.current = activeChatId;
  }, [activeChatId]);

  // Load chats from localStorage on mount
  useEffect(() => {
    const savedChats = localStorage.getItem("vaultmind_chats");
    const savedActiveId = localStorage.getItem("vaultmind_active_chat_id");

    let initialChats: Chat[] = [];
    let initialActiveId: string | null = null;

    if (savedChats) {
      try {
        initialChats = JSON.parse(savedChats) as Chat[];
      } catch (e) {
        console.error("Failed to parse saved chats:", e);
        initialChats = [];
      }
    }

    if (initialChats.length === 0) {
      const defaultChat: Chat = {
        id: Date.now().toString(),
        title: "New Chat",
        messages: [],
        createdAt: Date.now(),
      };
      initialChats = [defaultChat];
      initialActiveId = defaultChat.id;
    } else {
      initialActiveId =
        savedActiveId && initialChats.some((c) => c.id === savedActiveId)
          ? savedActiveId
          : initialChats[0].id;
    }

    setChats(initialChats);
    setActiveChatId(initialActiveId);
    setIsLoaded(true);
  }, []);

  // Save chats to localStorage on change
  useEffect(() => {
    if (isLoaded) {
      localStorage.setItem("vaultmind_chats", JSON.stringify(chats));
      if (activeChatId) {
        localStorage.setItem("vaultmind_active_chat_id", activeChatId);
      } else {
        localStorage.removeItem("vaultmind_active_chat_id");
      }
    }
  }, [chats, activeChatId, isLoaded]);

  // Poll backend health status
  useEffect(() => {
    checkHealth().then(setIsOnline);

    const interval = setInterval(() => {
      checkHealth().then(setIsOnline);
    }, 15000);

    return () => clearInterval(interval);
  }, []);

  // Get current active chat
  const activeChat: Chat | null = chats.find((c) => c.id === activeChatId) || null;
  const activeMessages = activeChat ? activeChat.messages : [];

  // Create a new empty chat session
  const createNewChat = (): void => {
    if (isStreaming) return;

    if (activeChat && activeChat.messages.length === 0) {
      return;
    }

    const newChat: Chat = {
      id: Date.now().toString(),
      title: "New Chat",
      messages: [],
      createdAt: Date.now(),
    };

    setChats((prev) => [newChat, ...prev]);
    setActiveChatId(newChat.id);
    setInput("");
  };

  // Switch to another chat session
  const selectChat = (id: string): void => {
    if (isStreaming) return;
    setActiveChatId(id);
    setInput("");
  };

  // Delete a chat session
  const deleteChat = (id: string, e?: React.MouseEvent): void => {
    if (e) {
      e.stopPropagation();
    }
    if (isStreaming) return;

    const remainingChats = chats.filter((c) => c.id !== id);

    if (remainingChats.length === 0) {
      const defaultChat: Chat = {
        id: Date.now().toString(),
        title: "New Chat",
        messages: [],
        createdAt: Date.now(),
      };
      setChats([defaultChat]);
      setActiveChatId(defaultChat.id);
    } else {
      setChats(remainingChats);
      if (activeChatId === id) {
        setActiveChatId(remainingChats[0].id);
      }
    }
  };

  // Send a message
  const sendMessage = async (messageText?: string): Promise<void> => {
    const textToSend = typeof messageText === "string" ? messageText : input;
    const trimmed = textToSend.trim();

    if (!trimmed || isStreaming) return;

    setInput("");
    setIsStreaming(true);

    const targetChatId = activeChatIdRef.current;

    setChats((prevChats) =>
      prevChats.map((c) => {
        if (c.id === targetChatId) {
          const isFirstMessage = c.messages.length === 0;
          const updatedTitle = isFirstMessage
            ? trimmed.slice(0, 30) + (trimmed.length > 30 ? "..." : "")
            : c.title;

          return {
            ...c,
            title: updatedTitle,
            messages: [
              ...c.messages,
              { role: "user" as const, content: trimmed },
              { role: "assistant" as const, content: "" },
            ],
          };
        }
        return c;
      })
    );

    try {
      await streamChat(
        trimmed,
        // onToken
        (token: string) => {
          setChats((prevChats) =>
            prevChats.map((c) => {
              if (c.id === targetChatId) {
                const updatedMessages = [...c.messages];
                const last = updatedMessages[updatedMessages.length - 1];
                if (last && last.role === "assistant") {
                  updatedMessages[updatedMessages.length - 1] = {
                    ...last,
                    content: last.content + token,
                  };
                }
                return { ...c, messages: updatedMessages };
              }
              return c;
            })
          );
        },
        // onDone
        () => {
          setIsStreaming(false);
        },
        // onError
        (err: Error) => {
          setChats((prevChats) =>
            prevChats.map((c) => {
              if (c.id === targetChatId) {
                const updatedMessages = [...c.messages];
                const last = updatedMessages[updatedMessages.length - 1];
                if (last && last.role === "assistant") {
                  updatedMessages[updatedMessages.length - 1] = {
                    ...last,
                    content:
                      last.content +
                      `\n\n⚠️ Failed to connect to VaultMind API. Make sure the backend is running.\n\nError: ${err.message}`,
                  };
                }
                return { ...c, messages: updatedMessages };
              }
              return c;
            })
          );
          setIsStreaming(false);
        }
      );
    } catch {
      setIsStreaming(false);
    }
  };

  return {
    chats,
    activeChatId,
    activeChat,
    activeMessages,
    isStreaming,
    input,
    setInput,
    isOnline,
    isLoaded,
    sendMessage,
    createNewChat,
    selectChat,
    deleteChat,
  };
}
