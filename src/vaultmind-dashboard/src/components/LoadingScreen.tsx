import React from "react";
import type { LoadingScreenProps } from "../types";
import spinnerIcon from "../../public/icons/spinner.svg";

export default function LoadingScreen({
  message = "Initializing VaultMind...",
}: LoadingScreenProps) {
  return (
    <div className="flex items-center justify-center h-screen bg-zinc-950 text-foreground select-none">
      <div className="flex flex-col items-center gap-3 animate-fade-in">
        <div
          className="animate-spin w-8 h-8 bg-current text-accent"
          style={{
            maskImage: `url(${spinnerIcon.src})`,
            WebkitMaskImage: `url(${spinnerIcon.src})`,
            maskSize: "contain",
            WebkitMaskSize: "contain",
            maskRepeat: "no-repeat",
            WebkitMaskRepeat: "no-repeat",
          }}
        />
        <span className="text-sm font-medium text-muted">{message}</span>
      </div>
    </div>
  );
}
