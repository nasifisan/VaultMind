# 🔒🧠 VaultMind

> An AI document intelligence platform powered by local inference, .NET orchestration, and real-time streaming.

**VaultMind** is a full-stack AI chat application that runs entirely on your local machine — no API keys, no cloud costs, no data leaving your computer. It uses [Ollama](https://ollama.com/) for local LLM inference, [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/) for orchestration, and a [Next.js](https://nextjs.org/) dashboard with real-time SSE streaming.

This is a learning project following an [AI System Architect roadmap](#-roadmap--whats-next), progressively building from a streaming chat app to a production-grade document intelligence platform with C++ inference, RAG pipelines, and containerized deployment.

---

## ✅ What's Built (Phase 1)

### Backend — C# .NET 9 API
- **Streaming chat endpoint** (`POST /api/chat`) using Server-Sent Events (SSE)
- **Microsoft Semantic Kernel** integration for LLM orchestration
- **Ollama** as the local inference provider (phi3 model, OpenAI-compatible API)
- **Health check endpoint** (`GET /api/health`) for frontend status monitoring
- Clean architecture: `Endpoints/`, `Services/`, `Interfaces/` separation

### Frontend — Next.js 16 Dashboard
- **Multi-chat support** — Create, switch, and delete chat sessions (like ChatGPT)
- **Real-time SSE streaming** — Tokens appear as they're generated, with typing indicator
- **Component-based architecture** — 7 reusable components, 1 custom hook, 1 service layer
- **Persistent chat history** — Conversations survive page reloads via localStorage
- **Live backend status** — Header shows Online/Offline/Thinking with color-coded indicators
- **Dark theme** with Tailwind CSS v4, Geist fonts, and smooth animations

---

## 🏗️ Current Architecture

```
┌───────────────────────────────────────────────┐
│         Next.js 16 Dashboard (App Router)      │
│    Multi-chat UI · SSE Streaming · Dark Theme  │
└─────────────────────┬─────────────────────────┘
                      │ HTTP + SSE
┌─────────────────────▼─────────────────────────┐
│            C# .NET 9 Web API                   │
│         Microsoft Semantic Kernel              │
│    ┌────────────┐    ┌──────────────────┐      │
│    │   Chat     │    │   Health Check   │      │
│    │  Endpoint  │    │    Endpoint      │      │
│    └─────┬──────┘    └──────────────────┘      │
└──────────┼────────────────────────────────────┘
           │ OpenAI-compatible API
┌──────────▼──────────┐
│       Ollama        │
│   phi3 (3.8B params)│
│   Local · Free      │
└─────────────────────┘
```

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | Next.js 16, React 19, Tailwind CSS v4 | Streaming chat UI with multi-session support |
| **Backend** | C# .NET 9, Semantic Kernel | API orchestration, SSE streaming |
| **LLM** | Ollama + phi3 | Free local inference, no API keys needed |
| **State** | localStorage | Client-side chat history persistence |

---

## 📁 Project Structure

```
VaultMind/
├── src/
│   ├── VaultMind.API/                # C# .NET Web API
│   │   ├── Program.cs                # Service registration & middleware
│   │   ├── Endpoints/
│   │   │   ├── ChatEndpoints.cs      # POST /api/chat (SSE streaming)
│   │   │   └── HealthEndpoints.cs    # GET /api/health
│   │   ├── Services/
│   │   │   └── SseService.cs         # SSE formatting & streaming logic
│   │   ├── Interfaces/
│   │   │   └── ISseService.cs        # SSE service contract
│   │   └── appsettings.json          # Ollama connection config
│   │
│   ├── vaultmind-dashboard/          # Next.js frontend
│   │   └── src/
│   │       ├── app/
│   │       │   ├── page.js           # Main page (layout composition)
│   │       │   ├── layout.js         # Root layout, fonts, metadata
│   │       │   └── globals.css       # Tailwind v4 theme & animations
│   │       ├── components/
│   │       │   ├── Header.js         # Branding, sidebar toggle, status
│   │       │   ├── Footer.js         # Bottom branding text
│   │       │   ├── Sidebar.js        # Chat history list panel
│   │       │   ├── ChatWindow.js     # Message viewport & welcome screen
│   │       │   ├── ChatMessage.js    # Individual message bubbles
│   │       │   ├── ChatInput.js      # Generic reusable text input
│   │       │   └── LoadingScreen.js  # Initialization spinner
│   │       ├── hooks/
│   │       │   └── useChatManager.js # Multi-chat state management
│   │       └── services/
│   │           └── chatService.js    # HTTP/SSE API communication
│   │
│   └── VaultMind.Engine/             # C++20 inference engine (planned)
│       ├── CMakeLists.txt
│       ├── include/
│       └── src/
│
├── infra/                            # Docker configs (planned)
├── docs/                             # Architecture documentation
├── VaultMind.sln                     # .NET solution file
└── README.md
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 9 SDK](https://dot.net/download)
- [Node.js 20+](https://nodejs.org)
- [Ollama](https://ollama.com/)

### 1. Set Up Ollama (One-Time)
```bash
# Install Ollama from https://ollama.com, then:
ollama pull phi3
```
Ollama runs automatically in the background after installation. The phi3 model (~2.2 GB) downloads once.

### 2. Start the Backend
```bash
cd src/VaultMind.API
dotnet run
```
The API starts on `http://localhost:5139`.

### 3. Start the Frontend
```bash
cd src/vaultmind-dashboard
npm install    # first time only
npm run dev
```
The dashboard opens at `http://localhost:3000`.

---

## 🧩 Key Design Decisions

| Decision | Why |
|----------|-----|
| **Ollama over OpenAI API** | Free, private, works offline. Can swap to OpenAI later without changing architecture. |
| **SSE over WebSockets** | Simpler for unidirectional streaming. The server pushes tokens; the client just reads. |
| **Semantic Kernel over direct API calls** | Microsoft's orchestration SDK — future-proof for plugins, RAG, and multi-model pipelines. |
| **localStorage over database** | Sufficient for a single-user learning project. No server-side session management needed. |
| **Component extraction** | Each UI piece is isolated and reusable. Service layer is the only place that calls `fetch()`. |

---

## 🗺️ Roadmap — What's Next

VaultMind is being built in phases following a 6-month AI System Architect roadmap:

| Phase | Focus | Status |
|-------|-------|--------|
| **Phase 1** | Streaming chat app + component architecture | ✅ Complete |
| **Phase 2** | RAG pipeline — document upload, embedding, vector search (Qdrant) | 🔜 Next |
| **Phase 3** | C++ inference engine with ONNX Runtime, .NET interop via P/Invoke | 📋 Planned |
| **Phase 4** | Capstone — full platform with Redis caching, metrics dashboard, Docker | 📋 Planned |

### Known Limitations (To Be Fixed)
- **No conversation memory** — Each message is sent without prior context. The LLM can't reference earlier messages in the same chat.
- **No authentication** — No JWT or API key protection on endpoints.
- **No document ingestion** — The "Document Intelligence" capability requires the Phase 2 RAG pipeline.

---

## 📄 License

MIT
