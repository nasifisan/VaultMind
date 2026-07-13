"use client";

import React, { forwardRef } from "react";
import type { ChatInputProps } from "../types";
import attachmentIcon from "../../public/icons/attachment.svg";
import spinnerIcon from "../../public/icons/spinner.svg";

const ChatInput = forwardRef<HTMLInputElement, ChatInputProps>(
  (
    {
      value,
      onChange,
      onSend,
      disabled,
      placeholder = "Ask VaultMind anything...",
      onFileSelect,
    },
    ref,
  ) => {
    const fileInputRef = React.useRef<HTMLInputElement>(null);

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        onSend();
      }
    };

    const handleAttachmentClick = () => {
      fileInputRef.current?.click();
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = e.target.files;
      if (files && files.length > 0) {
        onFileSelect(files[0]);
        e.target.value = "";
      }
    };

    const isButtonDisabled = !value.trim() || disabled;

    return (
      <div className="w-full flex gap-3 max-w-3xl mx-auto">
        <button
          onClick={handleAttachmentClick}
          type="button"
          disabled={disabled}
          className="p-3 rounded-xl bg-zinc-900 border border-border text-muted hover:text-foreground disabled:opacity-50 disabled:cursor-not-allowed hover:bg-surface-hover/50 transition-all duration-200 flex items-center justify-center cursor-pointer shrink-0"
          title="Upload document"
        >
          <div
            className="w-5 h-5 bg-current transition-colors duration-200"
            style={{
              maskImage: `url(${attachmentIcon.src})`,
              WebkitMaskImage: `url(${attachmentIcon.src})`,
              maskSize: "contain",
              WebkitMaskSize: "contain",
              maskRepeat: "no-repeat",
              WebkitMaskRepeat: "no-repeat",
            }}
          />
        </button>
        <input
          ref={fileInputRef}
          type="file"
          className="hidden"
          onChange={handleFileChange}
        />
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
          className="px-5 py-3 rounded-xl bg-accent hover:bg-accent-hover text-white text-sm font-medium disabled:opacity-30 disabled:cursor-not-allowed transition-all duration-200 hover:shadow-lg hover:shadow-accent/20 flex items-center justify-center min-w-[90px] cursor-pointer"
        >
          {disabled ? (
            <span className="flex items-center gap-2">
              <div
                className="animate-spin w-4 h-4 bg-current text-white"
                style={{
                  maskImage: `url(${spinnerIcon.src})`,
                  WebkitMaskImage: `url(${spinnerIcon.src})`,
                  maskSize: "contain",
                  WebkitMaskSize: "contain",
                  maskRepeat: "no-repeat",
                  WebkitMaskRepeat: "no-repeat",
                }}
              />
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
