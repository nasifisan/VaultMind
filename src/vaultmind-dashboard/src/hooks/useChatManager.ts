"use client";

import { useState, useEffect, useRef } from "react";
import {
  streamChat,
  getConversations,
  getConversation,
  saveConversation,
  deleteConversation,
  uploadDocument,
  getConversationDocuments,
  deleteDocument,
} from "../services/chatService.service";
import { useBackendHealth } from "./useBackendHealth";
import type {
  Chat,
  ChatManager,
  DocumentRecord,
  PendingDocument,
} from "../types";
import { generateGuid } from "@/shared/utils";
import { ConversationRole } from "@/types/conversation/conversation.contracts";

export default function useChatManager(): ChatManager {
  const [chats, setChats] = useState<Chat[]>([]);
  const [activeChatId, setActiveChatId] = useState<string | null>(null);
  const [isStreaming, setIsStreaming] = useState<boolean>(false);
  const [input, setInput] = useState<string>("");
  const [isLoaded, setIsLoaded] = useState<boolean>(false);
  const [documents, setDocuments] = useState<DocumentRecord[]>([]);
  const [pendingUploads, setPendingUploads] = useState<PendingDocument[]>([]);

  useEffect(() => {
    (documents || []).forEach((item) => {
      console.log("Documents:", item);
    });

    (pendingUploads || []).forEach((item) => {
      console.log("Pending Uploads:", item);
    });
  }, [documents, pendingUploads]);

  // Monitor API health
  const isOnline = useBackendHealth();

  // Active ID ref to prevent stale closures during streams
  const activeChatIdRef = useRef<string | null>(null);
  useEffect(() => {
    activeChatIdRef.current = activeChatId;
  }, [activeChatId]);

  // Load conversations on mount
  useEffect(() => {
    async function initChats() {
      try {
        const backendHeaders = await getConversations();

        let loadedChats: Chat[] = backendHeaders.map((h) => ({
          id: h.Id,
          title: h.Title,
          messages: [],
          createdAt: new Date(h.CreatedAt).getTime(),
        }));

        const savedActiveId = localStorage.getItem("vaultmind_active_chat_id");
        let activeId: string | null = null;

        if (loadedChats.length === 0) {
          const defaultChatId = generateGuid();
          await saveConversation(defaultChatId, "New Chat");

          loadedChats = [
            {
              id: defaultChatId,
              title: "New Chat",
              messages: [],
              createdAt: Date.now(),
            },
          ];
          activeId = defaultChatId;
        } else {
          activeId =
            savedActiveId && loadedChats.some((c) => c.id === savedActiveId)
              ? savedActiveId
              : loadedChats[0].id;
        }

        setChats(loadedChats);
        setActiveChatId(activeId);

        // Populate active chat messages
        if (activeId) {
          const detail = await getConversation(activeId);
          setChats((prev) =>
            syncConversationState(
              prev,
              activeId,
              detail.Title,
              detail.Messages,
            ),
          );
          // Load active chat documents
          try {
            const docs = await getConversationDocuments(activeId);
            setDocuments(docs);
          } catch (docErr) {
            console.error("Failed to load initial documents:", docErr);
          }
        }
      } catch (err) {
        console.error("Fallback to local storage...", err);
        const savedChats = localStorage.getItem("vaultmind_chats");
        const savedActiveId = localStorage.getItem("vaultmind_active_chat_id");
        let initialChats: Chat[] = savedChats ? JSON.parse(savedChats) : [];

        if (initialChats.length === 0) {
          const defaultId = generateGuid();
          initialChats = [
            {
              id: defaultId,
              title: "New Chat",
              messages: [],
              createdAt: Date.now(),
            },
          ];
        }

        setChats(initialChats);
        setActiveChatId(
          savedActiveId && initialChats.some((c) => c.id === savedActiveId)
            ? savedActiveId
            : initialChats[0].id,
        );
      } finally {
        setIsLoaded(true);
      }
    }

    initChats();
  }, []);

  // Sync active chat ID to localStorage
  useEffect(() => {
    if (isLoaded) {
      if (activeChatId) {
        localStorage.setItem("vaultmind_active_chat_id", activeChatId);
      } else {
        localStorage.removeItem("vaultmind_active_chat_id");
      }
    }
  }, [activeChatId, isLoaded]);

  const activeChat = chats.find((c) => c.id === activeChatId) || null;
  const activeMessages = activeChat ? activeChat.messages : [];

  // Actions
  const createNewChat = (): void => {
    if (isStreaming) return;
    if (activeChat && activeChat.messages.length === 0) return;

    const newChatId = generateGuid();
    const newChat: Chat = {
      id: newChatId,
      title: "New Chat",
      messages: [],
      createdAt: Date.now(),
    };

    setChats((prev) => [newChat, ...prev]);
    setActiveChatId(newChatId);
    setInput("");
    setDocuments([]);
    setPendingUploads([]);

    saveConversation(newChatId, "New Chat").catch((err) => console.error(err));
  };

  const selectChat = (id: string): void => {
    if (isStreaming) return;
    setActiveChatId(id);
    setInput("");
    setPendingUploads([]);

    getConversation(id)
      .then((detail) =>
        setChats((prev) =>
          syncConversationState(prev, id, detail.Title, detail.Messages),
        ),
      )
      .catch((err) => console.error(err));

    getConversationDocuments(id)
      .then((docs) => setDocuments(docs))
      .catch((err) => console.error("Failed to load documents for chat:", err));
  };

  const deleteChat = (id: string, e?: React.MouseEvent): void => {
    if (e) e.stopPropagation();
    if (isStreaming) return;

    const remainingChats = chats.filter((c) => c.id !== id);

    if (remainingChats.length === 0) {
      const defaultId = generateGuid();
      setChats([
        {
          id: defaultId,
          title: "New Chat",
          messages: [],
          createdAt: Date.now(),
        },
      ]);
      setActiveChatId(defaultId);
      setDocuments([]);
      setPendingUploads([]);
      saveConversation(defaultId, "New Chat").catch((err) =>
        console.error(err),
      );
    } else {
      setChats(remainingChats);
      if (activeChatId === id) {
        const nextId = remainingChats[0].id;
        setActiveChatId(nextId);
        setDocuments([]);
        setPendingUploads([]);
        getConversation(nextId)
          .then((detail) => {
            setChats((prev) =>
              syncConversationState(
                prev,
                nextId,
                detail.Title,
                detail.Messages,
              ),
            );
            return getConversationDocuments(nextId);
          })
          .then((docs) => setDocuments(docs))
          .catch((err) => console.error(err));
      }
    }

    deleteConversation(id).catch((err) => console.error(err));
  };

  const sendMessage = async (messageText?: string): Promise<void> => {
    const textToSend = typeof messageText === "string" ? messageText : input;
    const trimmed = textToSend.trim();

    if (!trimmed || isStreaming) return;

    setInput("");
    setIsStreaming(true);

    const targetChatId = activeChatIdRef.current;
    if (!targetChatId) {
      setIsStreaming(false);
      return;
    }

    // Append user & temporary empty assistant messages
    setChats((prev) => {
      const currentChat = prev.find((c) => c.id === targetChatId);
      const isFirst = currentChat ? currentChat.messages.length === 0 : true;
      const updatedTitle = isFirst
        ? trimmed.slice(0, 30) + (trimmed.length > 30 ? "..." : "")
        : currentChat?.title;

      const step1 = appendMessage(
        prev,
        targetChatId,
        ConversationRole.User,
        trimmed,
        updatedTitle,
      );
      return appendMessage(step1, targetChatId, ConversationRole.Assistant, "");
    });

    try {
      await streamChat(
        targetChatId,
        trimmed,
        // onToken
        (token) =>
          setChats((prev) =>
            appendTokenToLastMessage(prev, targetChatId, token),
          ),
        // onDone
        () => {
          setIsStreaming(false);
          // Sync full details from backend (captures finalized assistant text and title updates)
          getConversation(targetChatId)
            .then((detail) =>
              setChats((prev) =>
                syncConversationState(
                  prev,
                  targetChatId,
                  detail.Title,
                  detail.Messages,
                ),
              ),
            )
            .catch((err) => console.error(err));
        },
        // onError
        (err) => {
          setChats((prev) =>
            appendTokenToLastMessage(
              prev,
              targetChatId,
              `\n\n⚠️ Stream failure: ${err.message}`,
            ),
          );
          setIsStreaming(false);
        },
      );
    } catch {
      setIsStreaming(false);
    }
  };

  const uploadFile = async (file: File): Promise<void> => {
    const targetChatId = activeChatId;
    if (!targetChatId) return;

    const tempId =
      typeof crypto !== "undefined" && crypto.randomUUID
        ? crypto.randomUUID()
        : Math.random().toString(36).substring(2, 15);

    const pendingDoc: PendingDocument = {
      tempId,
      fileName: file.name,
      size: file.size,
      contentType: file.type,
      status: "uploading",
    };

    setPendingUploads((prev) => [...prev, pendingDoc]);
    console.log(targetChatId);

    try {
      const docRecord = await uploadDocument(targetChatId, file);
      setPendingUploads((prev) => prev.filter((d) => d.tempId !== tempId));
      setDocuments((prev) => [...prev, docRecord]);
    } catch (err) {
      console.error("Upload error:", err);
      setPendingUploads((prev) =>
        prev.map((d) => (d.tempId === tempId ? { ...d, status: "error" } : d)),
      );
    }
  };

  const deleteFile = async (id: string): Promise<void> => {
    try {
      await deleteDocument(id);
      setDocuments((prev) => prev.filter((d) => d.Id !== id));
    } catch (err) {
      console.error("Failed to delete document:", err);
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
    documents,
    pendingUploads,
    sendMessage,
    createNewChat,
    selectChat,
    deleteChat,
    uploadFile,
    deleteFile,
  };
}

// ── Pure State Reducers ──
const appendMessage = (
  chats: Chat[],
  chatId: string,
  role: ConversationRole,
  content: string,
  newTitle?: string,
): Chat[] =>
  chats.map((c) =>
    c.id === chatId
      ? {
          ...c,
          title: newTitle ?? c.title,
          messages: [...c.messages, { role, content }],
        }
      : c,
  );

const appendTokenToLastMessage = (
  chats: Chat[],
  chatId: string,
  token: string,
): Chat[] =>
  chats.map((c) => {
    if (c.id === chatId) {
      const updated = [...c.messages];
      const last = updated[updated.length - 1];
      if (last && last.role === ConversationRole.Assistant) {
        updated[updated.length - 1] = {
          ...last,
          content: last.content + token,
        };
      }
      return { ...c, messages: updated };
    }
    return c;
  });

const syncConversationState = (
  chats: Chat[],
  chatId: string,
  title: string,
  messages: { Role: string; Content: string }[],
): Chat[] =>
  chats.map((c) =>
    c.id === chatId
      ? {
          ...c,
          title,
          messages: messages.map((m) => ({
            role: m.Role as ConversationRole,
            content: m.Content,
          })),
        }
      : c,
  );
