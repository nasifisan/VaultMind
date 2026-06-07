// ── Chat Domain Types ──

export interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

export interface Chat {
  id: string;
  title: string;
  messages: ChatMessage[];
  createdAt: number;
}

// ── Chat Manager Hook Return Type ──

export interface ChatManager {
  chats: Chat[];
  activeChatId: string | null;
  activeChat: Chat | null;
  activeMessages: ChatMessage[];
  isStreaming: boolean;
  input: string;
  setInput: (value: string) => void;
  isOnline: boolean;
  isLoaded: boolean;
  sendMessage: (messageText?: string) => Promise<void>;
  createNewChat: () => void;
  selectChat: (id: string) => void;
  deleteChat: (id: string, e?: React.MouseEvent) => void;
}

// ── Component Props ──

export interface HeaderProps {
  isStreaming: boolean;
  isOnline: boolean;
  onToggleSidebar: () => void;
}

export interface FooterProps {}

export interface ChatInputProps {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onSend: () => void;
  disabled: boolean;
  placeholder?: string;
}

export interface ChatMessageProps {
  role: "user" | "assistant";
  content: string;
  isStreaming: boolean;
  isLast: boolean;
}

export interface ChatWindowProps {
  messages: ChatMessage[];
  isStreaming: boolean;
  onSuggestionClick: (suggestion: string) => void;
}

export interface SidebarProps {
  chats: Chat[];
  activeChatId: string | null;
  isOpen: boolean;
  isStreaming: boolean;
  onSelectChat: (id: string) => void;
  onNewChat: () => void;
  onDeleteChat: (id: string, e?: React.MouseEvent) => void;
}

export interface LoadingScreenProps {
  message?: string;
}
