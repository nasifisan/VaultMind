import React from "react";

export default function LoadingScreen({ message = "Initializing VaultMind..." }) {
  return (
    <div className="flex items-center justify-center h-screen bg-zinc-950 text-foreground select-none">
      <div className="flex flex-col items-center gap-3 animate-fade-in">
        <svg
          className="animate-spin w-8 h-8 text-accent"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
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
        <span className="text-sm font-medium text-muted">{message}</span>
      </div>
    </div>
  );
}
