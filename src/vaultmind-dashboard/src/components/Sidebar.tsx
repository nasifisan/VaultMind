"use client";

import type { SidebarProps } from "../types";
import plusIcon from "../../public/icons/plus.svg";
import chatIcon from "../../public/icons/chat.svg";
import trashIcon from "../../public/icons/trash.svg";

export default function Sidebar({
  chats,
  activeChatId,
  isOpen,
  isStreaming,
  onSelectChat,
  onNewChat,
  onDeleteChat,
}: SidebarProps) {
  return (
    <aside
      className={`fixed md:static inset-y-0 left-0 z-40 flex flex-col h-full bg-zinc-950 border-r border-border transition-all duration-300 ease-in-out ${
        isOpen
          ? "w-72 translate-x-0"
          : "w-0 -translate-x-full md:translate-x-0 md:w-0 overflow-hidden"
      }`}
    >
      {/* Sidebar Header with New Chat Button */}
      <div className="p-4 shrink-0">
        <button
          onClick={onNewChat}
          disabled={isStreaming}
          className="w-full py-3 px-4 rounded-xl border border-dashed border-border hover:border-accent/50 text-foreground hover:bg-surface-hover transition-all duration-200 flex items-center justify-center gap-2 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed select-none"
        >
          <div
            className="w-4 h-4 bg-accent"
            style={{
              maskImage: `url(${plusIcon.src})`,
              WebkitMaskImage: `url(${plusIcon.src})`,
              maskSize: "contain",
              WebkitMaskSize: "contain",
              maskRepeat: "no-repeat",
              WebkitMaskRepeat: "no-repeat",
            }}
          />
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
                  <div
                    className={`w-4 h-4 shrink-0 bg-current ${
                      isActive
                        ? "text-accent"
                        : "text-muted group-hover:text-foreground"
                    }`}
                    style={{
                      maskImage: `url(${chatIcon.src})`,
                      WebkitMaskImage: `url(${chatIcon.src})`,
                      maskSize: "contain",
                      WebkitMaskSize: "contain",
                      maskRepeat: "no-repeat",
                      WebkitMaskRepeat: "no-repeat",
                    }}
                  />
                  <span className="truncate">{chat.title}</span>
                </div>

                {/* Delete Button */}
                <button
                  onClick={(e) => onDeleteChat(chat.id, e)}
                  disabled={isStreaming}
                  className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-zinc-800 text-muted hover:text-rose-400 transition-all duration-200 disabled:opacity-30 disabled:pointer-events-none"
                  title="Delete chat"
                  aria-label="Delete chat"
                >
                  <div
                    className="w-4 h-4 bg-current"
                    style={{
                      maskImage: `url(${trashIcon.src})`,
                      WebkitMaskImage: `url(${trashIcon.src})`,
                      maskSize: "contain",
                      WebkitMaskSize: "contain",
                      maskRepeat: "no-repeat",
                      WebkitMaskRepeat: "no-repeat",
                    }}
                  />
                </button>
              </div>
            );
          })
        )}
      </div>

      {/* Sidebar Footer */}
      <div className="p-4 border-t border-border shrink-0 text-center select-none">
        <div className="text-[10px] text-muted tracking-widest uppercase font-semibold">
          VaultMind Dashboard
        </div>
      </div>
    </aside>
  );
}
