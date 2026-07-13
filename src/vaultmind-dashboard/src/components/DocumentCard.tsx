"use client";

import React from "react";
import pdfIcon from "../../public/icons/pdf.svg";
import imageIcon from "../../public/icons/image.svg";
import docIcon from "../../public/icons/document.svg";
import trashIcon from "../../public/icons/trash.svg";
import spinnerIcon from "../../public/icons/spinner.svg";
import warningIcon from "../../public/icons/warning.svg";

interface DocumentCardProps {
  name: string;
  size: number;
  contentType?: string;
  status?: "uploading" | "error" | "success";
  storageUrl?: string;
  onDelete?: () => void;
}

function formatBytes(bytes: number, decimals = 1) {
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + " " + sizes[i];
}

const getFileIcon = (contentType?: string) => {
  const type = contentType?.toLowerCase() || "";
  let iconSrc = docIcon.src;
  let iconColorClass = "bg-zinc-400";

  if (type.includes("pdf")) {
    iconSrc = pdfIcon.src;
    iconColorClass = "bg-red-500/80";
  } else if (type.includes("image")) {
    iconSrc = imageIcon.src;
    iconColorClass = "bg-emerald-500/80";
  } else if (type.includes("word") || type.includes("officedocument") || type.includes("text")) {
    iconSrc = docIcon.src;
    iconColorClass = "bg-sky-500/80";
  }

  return (
    <div
      className={`w-7 h-7 ${iconColorClass}`}
      style={{
        maskImage: `url(${iconSrc})`,
        WebkitMaskImage: `url(${iconSrc})`,
        maskSize: "contain",
        WebkitMaskSize: "contain",
        maskRepeat: "no-repeat",
        WebkitMaskRepeat: "no-repeat",
      }}
    />
  );
};

export default function DocumentCard({
  name,
  size,
  contentType,
  status = "success",
  storageUrl,
  onDelete,
}: DocumentCardProps) {
  return (
    <div className="group relative flex items-center gap-3 p-3 rounded-xl bg-surface/50 border border-border hover:border-accent/40 hover:bg-surface/80 transition-all duration-200 w-64 shrink-0 shadow-sm overflow-hidden select-none">
      {/* File Icon */}
      <div className="shrink-0 p-1 bg-surface rounded-lg border border-border/60">
        {getFileIcon(contentType)}
      </div>

      {/* File Info */}
      <div className="flex-1 min-w-0">
        {status === "success" && storageUrl ? (
          <a
            href={storageUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="block text-xs font-semibold text-foreground hover:text-accent transition-colors duration-150 truncate cursor-pointer"
            title={name}
          >
            {name}
          </a>
        ) : (
          <span
            className={`block text-xs font-semibold truncate ${
              status === "error" ? "text-red-400" : "text-muted"
            }`}
            title={name}
          >
            {name}
          </span>
        )}
        <span className="block text-[10px] text-muted/70 mt-0.5">
          {formatBytes(size)}
        </span>
      </div>

      {/* Status Indicators / Actions */}
      <div className="shrink-0 flex items-center gap-1.5 z-10">
        {status === "uploading" && (
          <div
            className="animate-spin h-3.5 w-3.5 bg-current text-accent"
            style={{
              maskImage: `url(${spinnerIcon.src})`,
              WebkitMaskImage: `url(${spinnerIcon.src})`,
              maskSize: "contain",
              WebkitMaskSize: "contain",
              maskRepeat: "no-repeat",
              WebkitMaskRepeat: "no-repeat",
            }}
          />
        )}
        {status === "error" && (
          <span className="text-red-400 flex items-center justify-center" title="Upload failed">
            <div
              className="w-4 h-4 bg-current"
              style={{
                maskImage: `url(${warningIcon.src})`,
                WebkitMaskImage: `url(${warningIcon.src})`,
                maskSize: "contain",
                WebkitMaskSize: "contain",
                maskRepeat: "no-repeat",
                WebkitMaskRepeat: "no-repeat",
              }}
            />
          </span>
        )}
        {status === "success" && onDelete && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              onDelete();
            }}
            className="opacity-0 group-hover:opacity-100 p-1.5 text-muted/60 hover:text-red-400 rounded-md hover:bg-red-500/10 transition-all duration-150 cursor-pointer flex items-center justify-center shrink-0"
            title="Delete document"
          >
            <div
              className="w-3.5 h-3.5 bg-current"
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
        )}
      </div>
    </div>
  );
}
