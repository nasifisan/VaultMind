# VaultMind Dashboard

> Next.js 16 frontend for VaultMind — a multi-chat AI interface with real-time SSE streaming and persistent chat history.

## What This Project Does

This is the user-facing dashboard that provides a ChatGPT-like experience connected to the VaultMind backend. It:

1. Manages **multiple chat sessions** — create, switch, rename, and delete conversations
2. **Streams AI responses** in real-time using Server-Sent Events from the .NET backend
3. **Persists chat history** to `localStorage` — survives page reloads and browser restarts
4. Monitors backend health with a **live status indicator** (Online / Offline / Thinking)

## Tech Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| Next.js | 16.2.7 | React framework with App Router |
| React | 19.2.4 | UI library |
| Tailwind CSS | v4 | Styling (`@theme inline` syntax) |
| Geist Font | — | Typography via `next/font` |

## Project Structure

```
src/
├── app/
│   ├── page.js              # Main page — composes all components
│   ├── layout.js            # Root layout, fonts, metadata, dark mode
│   └── globals.css          # Tailwind v4 theme variables & animations
│
├── components/
│   ├── Header.js            # Top bar: logo, sidebar toggle, status pill
│   ├── Footer.js            # Bottom branding text
│   ├── Sidebar.js           # Collapsible chat history panel
│   ├── ChatWindow.js        # Message viewport & welcome/suggestions screen
│   ├── ChatMessage.js       # Individual message bubble (user/assistant)
│   ├── ChatInput.js         # Generic reusable text input with send button
│   └── LoadingScreen.js     # Full-screen initialization spinner
│
├── hooks/
│   └── useChatManager.js    # Custom hook: multi-chat state, localStorage, health polling
│
└── services/
    └── chatService.js       # HTTP/SSE calls to backend (the only file that uses fetch)
```

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Single service file for API calls** | No component should call `fetch()` directly. All network logic is isolated in `chatService.js`. |
| **Custom hook over state library** | `useChatManager` keeps all chat state logic in one place without adding Redux/Zustand dependencies. |
| **`forwardRef` on ChatInput** | Allows `page.js` to programmatically focus the input after loading, chat switching, or stream completion. |
| **`isLoaded` guard** | Prevents hydration mismatch — localStorage is only available client-side, so we show a loading spinner until state is ready. |
| **Auto-naming chats** | The first user message (truncated to 30 chars) becomes the chat title automatically. |

## Environment Variables

Create a `.env.local` file in the project root:

```env
NEXT_PUBLIC_API_URL=http://localhost:5139
```

## Running

```bash
npm install    # first time only
npm run dev    # starts on http://localhost:3000
```

Make sure the [VaultMind.API](../VaultMind.API/) backend is running before using the chat.

## Building for Production

```bash
npm run build
npm start
```
