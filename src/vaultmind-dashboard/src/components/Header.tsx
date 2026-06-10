"use client";

import React from "react";
import type { HeaderProps } from "../types";
import menuIcon from "../../public/icons/menu.svg";

export default function Header({
  isStreaming,
  isOnline,
  onToggleSidebar,
}: HeaderProps) {
  return (
    <header className="flex items-center gap-3 px-6 py-4 border-b border-border bg-surface/50 backdrop-blur-sm">
      {/* Sidebar Toggle Button */}
      <button
        onClick={onToggleSidebar}
        className="p-2 -ml-2 rounded-lg hover:bg-surface-hover text-muted hover:text-foreground transition-colors duration-200"
        title="Toggle sidebar"
        aria-label="Toggle sidebar"
      >
        <div
          className="w-5 h-5 bg-current transition-colors duration-200"
          style={{
            maskImage: `url(${menuIcon.src})`,
            WebkitMaskImage: `url(${menuIcon.src})`,
            maskSize: "contain",
            WebkitMaskSize: "contain",
            maskRepeat: "no-repeat",
            WebkitMaskRepeat: "no-repeat",
          }}
        />
      </button>

      <div className="flex items-center gap-3">
        <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-accent/20 text-accent font-bold text-sm select-none">
          VM
        </div>
        <div>
          <h1 className="text-lg font-semibold tracking-tight select-none">
            VaultMind
          </h1>
          <p className="text-xs text-muted select-none">
            AI Document Intelligence
          </p>
        </div>
      </div>

      <div className="ml-auto flex items-center gap-2 bg-zinc-900/60 border border-border px-3 py-1.5 rounded-full select-none">
        <span
          className={`w-2 h-2 rounded-full ${
            !isOnline
              ? "bg-rose-500 animate-pulse"
              : isStreaming
                ? "bg-amber-400 cursor-blink"
                : "bg-emerald-400"
          }`}
        />
        <span className="text-xs font-medium text-muted">
          {!isOnline ? "Offline" : isStreaming ? "Thinking..." : "Online"}
        </span>
      </div>
    </header>
  );
}
