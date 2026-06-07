"use client";

import { useState, useEffect, useRef } from "react";
import { streamChat, checkHealth } from "../services/chatService";

export default function useChatManager() {
  const [chats, setChats] = useState([]);
  const [activeChatId, setActiveChatId] = useState(null);
  const [isStreaming, setIsStreaming] = useState(false);
  const [input, setInput] = useState("");
  const [isOnline, setIsOnline] = useState(true);
  const [isLoaded, setIsLoaded] = useState(false);

  // Keep a ref to the activeChatId so callbacks in streamChat always have the latest value
  const activeChatIdRef = useRef(null);
  useEffect(() => {
    activeChatIdRef.current = activeChatId;
  }, [activeChatId]);

  // Load chats from localStorage on mount
  useEffect(() => {
    const savedChats = localStorage.getItem("vaultmind_chats");
    const savedActiveId = localStorage.getItem("vaultmind_active_chat_id");

    let initialChats = [];
    let initialActiveId = null;

    if (savedChats) {
      try {
        initialChats = JSON.parse(savedChats);
      } catch (e) {
        console.error("Failed to parse saved chats:", e);
        initialChats = [];
      }
    }

    if (initialChats.length === 0) {
      const defaultChat = {
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
  const activeChat = chats.find((c) => c.id === activeChatId) || null;
  const activeMessages = activeChat ? activeChat.messages : [];

  // Create a new empty chat session
  const createNewChat = () => {
    if (isStreaming) return;

    // Check if the current active chat is already empty. If so, just keep using it.
    if (activeChat && activeChat.messages.length === 0) {
      return;
    }

    const newChat = {
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
  const selectChat = (id) => {
    if (isStreaming) return;
    setActiveChatId(id);
    setInput("");
  };

  // Delete a chat session
  const deleteChat = (id, e) => {
    if (e) {
      e.stopPropagation(); // Prevent selecting the chat when clicking delete
    }
    if (isStreaming) return;

    const remainingChats = chats.filter((c) => c.id !== id);

    if (remainingChats.length === 0) {
      const defaultChat = {
        id: Date.now().toString(),
        title: "New Chat",
        messages: [],
        createdAt: Date.now(),
      };
      setChats([defaultChat]);
      setActiveChatId(defaultChat.id);
    } else {
      setChats(remainingChats);
      // If we deleted the active chat, switch to the first remaining one
      if (activeChatId === id) {
        setActiveChatId(remainingChats[0].id);
      }
    }
  };

  // Send a message
  const sendMessage = async (messageText) => {
    const textToSend = typeof messageText === "string" ? messageText : input;
    const trimmed = textToSend.trim();

    if (!trimmed || isStreaming) return;

    // Clear input first
    setInput("");
    setIsStreaming(true);

    const targetChatId = activeChatIdRef.current;

    // Update messages to append user input and a placeholder assistant message
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
              { role: "user", content: trimmed },
              { role: "assistant", content: "" },
            ],
          };
        }
        return c;
      })
    );

    try {
      await streamChat(
        trimmed,
        // onToken callback
        (token) => {
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
        // onDone callback
        () => {
          setIsStreaming(false);
        },
        // onError callback
        (err) => {
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
    } catch (err) {
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
