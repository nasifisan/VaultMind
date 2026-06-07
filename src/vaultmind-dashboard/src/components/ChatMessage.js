"use client";

import React from "react";

export default function ChatMessage({ role, content, isStreaming, isLast }) {
  const isAssistant = role === "assistant";

  return (
    <div
      className={`flex gap-3 animate-fade-in ${
        isAssistant ? "justify-start" : "justify-end"
      }`}
    >
      {/* Assistant Avatar */}
      {isAssistant && (
        <div className="flex-shrink-0 w-8 h-8 rounded-lg bg-accent/20 text-accent flex items-center justify-center text-xs font-bold mt-1 select-none">
          VM
        </div>
      )}

      {/* Message Bubble */}
      <div
        className={`max-w-[80%] px-4 py-3 rounded-2xl text-sm leading-relaxed whitespace-pre-wrap shadow-sm ${
          !isAssistant
            ? "bg-accent text-white rounded-br-md"
            : "bg-surface border border-border rounded-bl-md text-foreground"
        }`}
      >
        {content}
        {isAssistant && isStreaming && isLast && (
          <span className="cursor-blink ml-0.5 text-accent font-semibold">▊</span>
        )}
      </div>

      {/* User Avatar */}
      {!isAssistant && (
        <div className="flex-shrink-0 w-8 h-8 rounded-lg bg-zinc-700 text-zinc-300 flex items-center justify-center text-xs font-bold mt-1 select-none">
          You
        </div>
      )}
    </div>
  );
}
