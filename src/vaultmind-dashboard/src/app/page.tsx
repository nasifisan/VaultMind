"use client";

import { useState, useRef, useEffect } from "react";
import useChatManager from "../hooks/useChatManager";
import Sidebar from "../components/Sidebar";
import Header from "../components/Header";
import ChatWindow from "../components/ChatWindow";
import ChatInput from "../components/ChatInput";
import Footer from "../components/Footer";
import LoadingScreen from "../components/LoadingScreen";
import { authService } from "@/services/authService.service";

export default function Home() {
  const {
    chats,
    activeChatId,
    activeMessages,
    isStreaming,
    input,
    setInput,
    isOnline,
    isLoaded,
    sendMessage,
    createNewChat,
    selectChat,
    deleteChat,
  } = useChatManager();

  const [sidebarOpen, setSidebarOpen] = useState<boolean>(true);
  const inputRef = useRef<HTMLInputElement>(null);

  // Proactively fetch or refresh token on application mount/reload
  useEffect(() => {
    const tokens = authService.getTokens();
    authService.requestToken(tokens?.RefreshToken).catch((err) => {
      console.warn("Could not auto-initialize or refresh session token on app load.", err);
    });
  }, []);

  // Focus input when active chat changes or streaming finishes
  useEffect(() => {
    if (isLoaded && !isStreaming) {
      inputRef.current?.focus();
    }
  }, [activeChatId, isStreaming, isLoaded]);

  // Handle suggestion click
  const handleSuggestionClick = (suggestion: string): void => {
    sendMessage(suggestion);
  };

  // Prevent UI flashing during client-side hydration of localStorage
  if (!isLoaded) {
    return <LoadingScreen message="Initializing VaultMind..." />;
  }

  return (
    <div className="flex h-screen overflow-hidden bg-zinc-950 text-foreground">
      {/* Mobile Sidebar Backdrop */}
      {sidebarOpen && (
        <div
          onClick={() => setSidebarOpen(false)}
          className="fixed inset-0 bg-black/60 z-30 md:hidden"
        />
      )}

      {/* Sidebar - Collapsible chat list */}
      <Sidebar
        chats={chats}
        activeChatId={activeChatId}
        isOpen={sidebarOpen}
        isStreaming={isStreaming}
        onSelectChat={selectChat}
        onNewChat={createNewChat}
        onDeleteChat={deleteChat}
      />

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col h-full min-w-0 overflow-hidden relative">
        {/* Header - contains status & sidebar toggle */}
        <Header
          isStreaming={isStreaming}
          isOnline={isOnline}
          onToggleSidebar={() => setSidebarOpen(!sidebarOpen)}
        />

        {/* Chat Window - shows message log & auto-scroll */}
        <ChatWindow
          messages={activeMessages}
          isStreaming={isStreaming}
          onSuggestionClick={handleSuggestionClick}
        />

        {/* Footer Area - input bar and backend info */}
        <div className="border-t border-border bg-surface/50 backdrop-blur-sm px-4 py-4 shrink-0">
          <ChatInput
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onSend={sendMessage}
            disabled={isStreaming}
            placeholder="Ask VaultMind anything..."
          />
          <Footer />
        </div>
      </div>
    </div>
  );
}
