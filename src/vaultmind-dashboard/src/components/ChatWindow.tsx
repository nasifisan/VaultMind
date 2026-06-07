"use client";

import React, { useEffect, useRef } from "react";
import ChatMessage from "./ChatMessage";
import type { ChatWindowProps } from "../types";

const suggestions: string[] = [
  "What can you do?",
  "Explain RAG architecture",
  "What is ONNX Runtime?",
];

export default function ChatWindow({ messages, isStreaming, onSuggestionClick }: ChatWindowProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom when messages list changes or streaming content updates
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isStreaming]);

  return (
    <div className="flex-1 overflow-y-auto px-4 py-6">
      <div className="max-w-3xl mx-auto space-y-6">
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full min-h-[60vh] gap-4 text-center">
            <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-accent/10 text-accent text-2xl font-bold select-none">
              VM
            </div>
            <h2 className="text-xl font-semibold select-none">Welcome to VaultMind</h2>
            <p className="text-muted max-w-md select-none">
              Ask me anything. I&apos;m your AI document intelligence assistant,
              powered by streaming inference.
            </p>
            <div className="flex flex-wrap gap-2 justify-center mt-4">
              {suggestions.map((suggestion) => (
                <button
                  key={suggestion}
                  onClick={() => onSuggestionClick(suggestion)}
                  disabled={isStreaming}
                  className="px-4 py-2 text-sm rounded-full border border-border text-muted hover:text-foreground hover:border-accent/50 hover:bg-surface-hover transition-all duration-200 disabled:opacity-50 disabled:pointer-events-none"
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        ) : (
          messages.map((msg, index) => (
            <ChatMessage
              key={index}
              role={msg.role}
              content={msg.content}
              isStreaming={isStreaming}
              isLast={index === messages.length - 1}
            />
          ))
        )}
        <div ref={messagesEndRef} />
      </div>
    </div>
  );
}
