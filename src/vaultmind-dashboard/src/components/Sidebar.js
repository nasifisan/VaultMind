"use client";

import React from "react";

export default function Sidebar({
  chats,
  activeChatId,
  isOpen,
  isStreaming,
  onSelectChat,
  onNewChat,
  onDeleteChat,
}) {
  return (
    <aside
      className={`fixed md:static inset-y-0 left-0 z-40 flex flex-col h-full bg-zinc-950 border-r border-border transition-all duration-300 ease-in-out ${
        isOpen ? "w-72 translate-x-0" : "w-0 -translate-x-full md:translate-x-0 md:w-0 overflow-hidden"
      }`}
    >
      {/* Sidebar Header with New Chat Button */}
      <div className="p-4 flex-shrink-0">
        <button
          onClick={onNewChat}
          disabled={isStreaming}
          className="w-full py-3 px-4 rounded-xl border border-dashed border-border hover:border-accent/50 text-foreground hover:bg-surface-hover transition-all duration-200 flex items-center justify-center gap-2 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed select-none"
        >
          <svg
            className="w-4 h-4 text-accent"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2.5"
              d="M12 4v16m8-8H4"
            />
          </svg>
          New Chat
        </button>
      </div>

      {/* Chat List */}
      <div className="flex-1 overflow-y-auto px-3 py-2 space-y-1 select-none">
        <div className="text-[11px] font-semibold text-muted px-3 uppercase tracking-wider mb-2">
          Chat History
        </div>

        {chats.length === 0 ? (
          <div className="text-xs text-muted text-center py-8 px-4">
            No chats yet. Start one above!
          </div>
        ) : (
          chats.map((chat) => {
            const isActive = chat.id === activeChatId;
            return (
              <div
                key={chat.id}
                onClick={() => onSelectChat(chat.id)}
                className={`group flex items-center justify-between px-3 py-2.5 rounded-lg text-sm cursor-pointer transition-all duration-200 ${
                  isActive
                    ? "bg-surface text-foreground font-medium border border-border"
                    : "text-muted hover:text-foreground hover:bg-surface-hover border border-transparent"
                }`}
              >
                {/* Chat Title */}
                <div className="flex items-center gap-2.5 min-w-0 flex-1 pr-2">
                  <svg
                    className={`w-4 h-4 flex-shrink-0 ${
                      isActive ? "text-accent" : "text-muted group-hover:text-foreground"
                    }`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
                    />
                  </svg>
                  <span className="truncate">{chat.title}</span>
                </div>

                {/* Delete Button (visible on hover for active/inactive, or always on touch devices) */}
                <button
                  onClick={(e) => onDeleteChat(chat.id, e)}
                  disabled={isStreaming}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-zinc-800 text-muted hover:text-rose-400 transition-all duration-200 disabled:opacity-30 disabled:pointer-events-none"
                  title="Delete chat"
                  aria-label="Delete chat"
                >
                  <svg
                    className="w-4 h-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                </button>
              </div>
            );
          })
        )}
      </div>

      {/* Sidebar Footer */}
      <div className="p-4 border-t border-border flex-shrink-0 text-center select-none">
        <div className="text-[10px] text-muted tracking-widest uppercase font-semibold">
          VaultMind Dashboard
        </div>
      </div>
    </aside>
  );
}
