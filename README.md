# 🔒🧠 VaultMind

> An AI document intelligence platform powered by local inference, .NET orchestration, and real-time streaming.

**VaultMind** is a full-stack AI chat application that runs entirely on your local machine — no API keys, no cloud costs, no data leaving your computer. It uses [Ollama](https://ollama.com/) for local LLM inference, [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/) for orchestration, and a [Next.js](https://nextjs.org/) dashboard with real-time SSE streaming.

This is a learning project following an [AI System Architect roadmap](#-roadmap--whats-next), progressively building from a streaming chat app to a production-grade document intelligence platform with C++ inference, RAG pipelines, and containerized deployment.

---

## ✅ What's Built (Phase 1)

### Backend — C# .NET 9 API
- **Controller-Based Routing**: Clean MVC controllers (`AuthController`, `ChatController`, `HealthController`) replacing minimal endpoints.
- **Conversation Memory**: Rebuilds conversation history dynamically into the Semantic Kernel `ChatHistory` object on every request.
- **Reusable Prompt Templates**: File-based prompts (`skprompt.txt` / `config.json`) stored in a `Prompts` directory and copied to the build folder.
- **Native C# Plugins**: Created `UtilityPlugin.cs` containing native functions (`GetCurrentTime` and `SummarizeText`) registered directly on the `Kernel`.
- **MongoDB Integration**: Local database instance mapping collections (`Users`, `RefreshTokens`, `ActiveAccessTokens`) with a generic repository pattern and automatic TTL index creation on startup.
- **JWT Authentication**: Full signup, signin, token validation, and token refresh/revocation endpoints.
- **Ollama Orchestration**: Microsoft Semantic Kernel connector mapping local inference via the OpenAI-compatible v1 endpoint.

### Frontend — Next.js 16 Dashboard
- **TypeScript Migration**: Full frontend migration to TypeScript for absolute type safety.
- **Centralized Fetch Interceptor (`apiFetch`)**: Custom fetch client that automatically:
  - Appends Bearer JWT tokens.
  - Intercepts `401 Unauthorized` responses and locks concurrent requests.
  - Refreshes tokens via the backend and retries the original calls.
  - Falls back to guest/anonymous sessions automatically.
- **Startup Auth Initialization**: Mount hook proactively initializes/refreshes guest tokens on app load.
- **Multi-chat support**: Create, switch, and delete chat sessions (persisted via localStorage).
- **Real-time SSE streaming**: Token-by-token rendering with a live status indicator (Online/Offline/Thinking).
- **Dark theme** with Tailwind CSS v4, Geist fonts, and smooth animations.

---

## 🏗️ Current Architecture

```
┌───────────────────────────────────────────────┐
│         Next.js 16 Dashboard (App Router)      │
│    Multi-chat UI · SSE Streaming · Dark Theme  │
└─────────────────────┬─────────────────────────┘
                      │ HTTP + SSE (with JWT Auth)
┌─────────────────────▼─────────────────────────┐
│            C# .NET 9 Web API (Controllers)    │
│         Microsoft Semantic Kernel (Kernel)    │
│       ┌───────────────────┬──────────────────┐│
│       │  AuthController   │  ChatController  ││
│       └─────────┬─────────┴────────┬─────────┘│
└─────────────────┼──────────────────┼──────────┘
                  │                  │
      MongoDB     │ JWT/TTL          │ OpenAI API
   ┌───────────┐◄─┘                  └───► ┌───────────┐
   │  Local DB │                           │  Ollama   │
   │ (Docker)  │                           │   phi3    │
   └───────────┘                           └───────────┘
```

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | Next.js 16, React 19, TypeScript, Tailwind CSS v4 | Streaming chat UI with multi-session support |
| **Backend** | C# .NET 9, Semantic Kernel | API orchestration, SSE streaming |
| **Database** | MongoDB (local Docker container) | User registry, Access & Refresh token tracking |
| **LLM** | Ollama + phi3 | Free local inference, no API keys needed |
| **State** | localStorage | Client-side chat history persistence |

---

## 📁 Project Structure

```
VaultMind/
├── src/
│   ├── VaultMind.API/                # C# .NET Web API
│   │   ├── Program.cs                # Service registration & middleware
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs     # JWT signup, signin, token endpoint
│   │   │   ├── ChatController.cs     # POST /api/chat (prompt template streaming)
│   │   │   └── HealthController.cs   # GET /api/health
│   │   ├── Plugins/
│   │   │   └── UtilityPlugin.cs      # Native C# tools (clock, summarize)
│   │   ├── Prompts/
│   │   │   └── ChatPlugin/           # Dynamic prompt templates
│   │   ├── Services/
│   │   │   ├── JwtService.cs         # JWT token generator
│   │   │   ├── MongoDbContext.cs     # MongoDB connector context
│   │   │   ├── MongoRepository.cs    # Generic repository pattern
│   │   │   └── SseService.cs         # SSE formatting & streaming logic
│   │   ├── Interfaces/
│   │   │   └── ...                   # API service contracts
│   │   └── appsettings.json          # MongoDB & Ollama connection config
│   │
│   ├── vaultmind-dashboard/          # Next.js frontend
│   │   └── src/
│   │       ├── app/
│   │       │   ├── page.tsx          # Main page (layout composition)
│   │       │   ├── layout.tsx        # Root layout, fonts, metadata
│   │       │   └── globals.css       # Tailwind v4 theme & animations
│   │       ├── components/
│   │       │   ├── Header.tsx        # Branding, sidebar toggle, status
│   │       │   ├── Footer.tsx        # Bottom branding text
│   │       │   ├── Sidebar.tsx       # Chat history list panel
│   │       │   ├── ChatWindow.tsx    # Message viewport & welcome screen
│   │       │   ├── ChatMessage.tsx   # Individual message bubbles
│   │       │   ├── ChatInput.tsx     # Generic reusable text input
│   │       │   └── LoadingScreen.tsx # Initialization spinner
│   │       ├── hooks/
│   │       │   └── useChatManager.ts # Multi-chat state management
│   │       └── services/
│   │           ├── apiClient.ts      # Centralized apiFetch interceptor
│   │           ├── authService.ts    # JWT storage & endpoints caller
│   │           └── chatService.service.ts # Chat streaming communication
│   │
│   └── VaultMind.Engine/             # C++20 inference engine (planned)
│       ├── CMakeLists.txt
│       ├── include/
│       └── src/
│
├── local/                            # Local infrastructure configs
│   └── mongodb/
│       └── Dockerfile                # Custom Dockerfile for MongoDB
│
├── infra/                            # Docker configs (planned)
├── docs/                             # Architecture documentation
├── VaultMind.sln                     # .NET solution file
├── nuget.config                      # Custom NuGet source (speeds up builds)
└── README.md
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 9 SDK](https://dot.net/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Ollama](https://ollama.com/)

### 1. Set Up Local Dependencies (Docker & Ollama)
Run the provided utility scripts in your PowerShell console to quickly set up your local environment:
```powershell
# Start Docker Desktop and launch the local MongoDB container named 'mongodb' on port 27017
powershell -ExecutionPolicy Bypass -File .\src\start-mongodb.ps1

# Start the Ollama background service and download the 'phi3' model
powershell -ExecutionPolicy Bypass -File .\src\start-ollama.ps1
```

### 2. Start the Backend API
```bash
cd src/VaultMind.API
dotnet run
```
The API starts on `http://localhost:5152`.

### 3. Start the Frontend Dashboard
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
| **Lightweight DbContext** | MongoDB Context exposes only a generic database hook; repository manages collection mappings dynamically. |
| **Client-Side Interception (`apiFetch`)** | Centralized client-side middleware manages token injection, concurrency locks, and automatic refreshes transparently. |
| **File-Based Prompts** | Keeps prompt engineering clean and decoupled from C# compilation. |

---

## 🗺️ Roadmap — What's Next

VaultMind is being built in phases following a 6-month AI System Architect roadmap:

| Phase | Focus | Status |
|-------|-------|--------|
| **Phase 1** | Streaming chat app + auth + conversation memory + prompt templates | ✅ Complete |
| **Phase 2** | RAG pipeline — document upload, embedding, vector search (Qdrant) | 🔜 Next |
| **Phase 3** | C++ inference engine with ONNX Runtime, .NET interop via P/Invoke | 📋 Planned |
| **Phase 4** | Capstone — full platform with Redis caching, metrics dashboard, Docker | 📋 Planned |

### In-Progress / Known Limitations (To Be Fixed in Phase 2)
- **Local LLM Tool calling** — Since `phi3` on Ollama does not natively support tool calling (HTTP 400), we must adapt custom plugins using context injection (RAG) rather than auto-invocation.
- **No document ingestion** — The "Document Intelligence" capability requires the Phase 2 RAG pipeline (document upload, chunking, and embedding).

---

## 📄 License

MIT
