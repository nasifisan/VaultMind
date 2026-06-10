"use client";

import React, { forwardRef } from "react";
import type { ChatInputProps } from "../types";

const ChatInput = forwardRef<HTMLInputElement, ChatInputProps>(
  (
    {
      value,
      onChange,
      onSend,
      disabled,
      placeholder = "Ask VaultMind anything...",
    },
    ref,
  ) => {
    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        onSend();
      }
    };

    const isButtonDisabled = !value.trim() || disabled;

    return (
      <div className="w-full flex gap-3 max-w-3xl mx-auto">
        <input
          ref={ref}
          type="text"
          value={value}
          onChange={onChange}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          className="flex-1 px-4 py-3 rounded-xl bg-zinc-900 border border-border text-sm text-foreground placeholder-muted focus:outline-none focus:ring-2 focus:ring-accent/55 focus:border-accent/55 disabled:opacity-50 transition-all duration-200"
        />
        <button
          onClick={onSend}
          disabled={isButtonDisabled}
          className="px-5 py-3 rounded-xl bg-accent hover:bg-accent-hover text-white text-sm font-medium disabled:opacity-30 disabled:cursor-not-allowed transition-all duration-200 hover:shadow-lg hover:shadow-accent/20 flex items-center justify-center min-w-[90px]"
        >
          {disabled ? (
            <span className="flex items-center gap-2">
              <svg
                className="animate-spin w-4 h-4 text-white"
                viewBox="0 0 24 24"
                fill="none"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                />
              </svg>
              Stream
            </span>
          ) : (
            "Send"
          )}
        </button>
      </div>
    );
  },
);

ChatInput.displayName = "ChatInput";

export default ChatInput;
