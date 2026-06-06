"use client";

import { useState, useRef, useEffect } from "react";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5139";

export default function Home() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");
  const [isStreaming, setIsStreaming] = useState(false);
  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Focus input on load
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  async function handleSend() {
    const trimmed = input.trim();
    if (!trimmed || isStreaming) return;

    // Add user message to chat
    const userMessage = { role: "user", content: trimmed };
    setMessages((prev) => [...prev, userMessage]);
    setInput("");
    setIsStreaming(true);

    // Add empty assistant message (will be filled by stream)
    const assistantMessage = { role: "assistant", content: "" };
    setMessages((prev) => [...prev, assistantMessage]);

    try {
      const response = await fetch(`${API_URL}/api/chat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: trimmed }),
      });

      if (!response.ok) {
        throw new Error(`API error: ${response.status}`);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const text = decoder.decode(value, { stream: true });
        const lines = text.split("\n");

        for (const line of lines) {
          if (line.startsWith("data: ")) {
            const data = line.slice(6);

            if (data === "[DONE]") break;
            if (data.startsWith("[ERROR]")) {
              setMessages((prev) => {
                const updated = [...prev];
                updated[updated.length - 1] = {
                  role: "assistant",
                  content: `⚠️ Error: ${data.slice(8)}`,
                };
                return updated;
              });
              break;
            }

            // Append token to the last assistant message
            setMessages((prev) => {
              const updated = [...prev];
              const last = updated[updated.length - 1];
              updated[updated.length - 1] = {
                ...last,
                content: last.content + data,
              };
              return updated;
            });
          }
        }
      }
    } catch (error) {
      setMessages((prev) => {
        const updated = [...prev];
        updated[updated.length - 1] = {
          role: "assistant",
          content: `⚠️ Failed to connect to VaultMind API. Make sure the backend is running.\n\nError: ${error.message}`,
        };
        return updated;
      });
    } finally {
      setIsStreaming(false);
      inputRef.current?.focus();
    }
  }

  function handleKeyDown(e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  return (
    <div className="flex flex-col h-screen">
      {/* ── Header ── */}
      <header className="flex items-center gap-3 px-6 py-4 border-b border-border bg-surface/50 backdrop-blur-sm">
        <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-accent/20 text-accent font-bold text-sm">
          VM
        </div>
        <div>
          <h1 className="text-lg font-semibold tracking-tight">VaultMind</h1>
          <p className="text-xs text-muted">AI Document Intelligence</p>
        </div>
        <div className="ml-auto flex items-center gap-2">
          <span className={`w-2 h-2 rounded-full ${isStreaming ? "bg-amber-400 cursor-blink" : "bg-emerald-400"}`} />
          <span className="text-xs text-muted">
            {isStreaming ? "Thinking..." : "Online"}
          </span>
        </div>
      </header>

      {/* ── Messages ── */}
      <main className="flex-1 overflow-y-auto px-4 py-6">
        <div className="max-w-3xl mx-auto space-y-6">
          {messages.length === 0 && (
            <div className="flex flex-col items-center justify-center h-full min-h-[60vh] gap-4 text-center">
              <div className="flex items-center justify-center w-16 h-16 rounded-2xl bg-accent/10 text-accent text-2xl font-bold">
                VM
              </div>
              <h2 className="text-xl font-semibold">Welcome to VaultMind</h2>
              <p className="text-muted max-w-md">
                Ask me anything. I&apos;m your AI document intelligence assistant,
                powered by streaming inference.
              </p>
              <div className="flex flex-wrap gap-2 mt-4">
                {[
                  "What can you do?",
                  "Explain RAG architecture",
                  "What is ONNX Runtime?",
                ].map((suggestion) => (
                  <button
                    key={suggestion}
                    onClick={() => {
                      setInput(suggestion);
                      inputRef.current?.focus();
                    }}
                    className="px-4 py-2 text-sm rounded-full border border-border text-muted hover:text-foreground hover:border-accent/50 hover:bg-surface-hover transition-all duration-200"
                  >
                    {suggestion}
                  </button>
                ))}
              </div>
            </div>
          )}

          {messages.map((msg, index) => (
            <div
              key={index}
              className={`flex gap-3 animate-fade-in ${
                msg.role === "user" ? "justify-end" : "justify-start"
              }`}
            >
              {msg.role === "assistant" && (
                <div className="flex-shrink-0 w-8 h-8 rounded-lg bg-accent/20 text-accent flex items-center justify-center text-xs font-bold mt-1">
                  VM
                </div>
              )}
              <div
                className={`max-w-[80%] px-4 py-3 rounded-2xl text-sm leading-relaxed whitespace-pre-wrap ${
                  msg.role === "user"
                    ? "bg-accent text-white rounded-br-md"
                    : "bg-surface border border-border rounded-bl-md"
                }`}
              >
                {msg.content}
                {msg.role === "assistant" &&
                  isStreaming &&
                  index === messages.length - 1 && (
                    <span className="cursor-blink ml-0.5 text-accent">▊</span>
                  )}
              </div>
              {msg.role === "user" && (
                <div className="flex-shrink-0 w-8 h-8 rounded-lg bg-zinc-700 text-zinc-300 flex items-center justify-center text-xs font-bold mt-1">
                  You
                </div>
              )}
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>
      </main>

      {/* ── Input ── */}
      <footer className="border-t border-border bg-surface/50 backdrop-blur-sm px-4 py-4">
        <div className="max-w-3xl mx-auto flex gap-3">
          <input
            ref={inputRef}
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask VaultMind anything..."
            disabled={isStreaming}
            className="flex-1 px-4 py-3 rounded-xl bg-zinc-900 border border-border text-sm text-foreground placeholder-muted focus:outline-none focus:ring-2 focus:ring-accent/50 focus:border-accent/50 disabled:opacity-50 transition-all duration-200"
          />
          <button
            onClick={handleSend}
            disabled={!input.trim() || isStreaming}
            className="px-5 py-3 rounded-xl bg-accent hover:bg-accent-hover text-white text-sm font-medium disabled:opacity-30 disabled:cursor-not-allowed transition-all duration-200 hover:shadow-lg hover:shadow-accent/20"
          >
            {isStreaming ? (
              <span className="flex items-center gap-2">
                <svg className="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Streaming
              </span>
            ) : (
              "Send"
            )}
          </button>
        </div>
        <p className="text-center text-xs text-muted mt-2">
          VaultMind streams responses via SSE from the .NET Semantic Kernel backend
        </p>
      </footer>
    </div>
  );
}
