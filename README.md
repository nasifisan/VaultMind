# 🔒🧠 VaultMind

> An AI document intelligence platform powered by local inference, .NET orchestration, and real-time streaming.

**VaultMind** is a full-stack AI document intelligence platform that runs on your local machine. It uses [Ollama](https://ollama.com/) for local LLM inference and embeddings, [Microsoft Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/) for orchestration, [Qdrant](https://qdrant.tech/) for vector search, [Google Cloud Storage](https://cloud.google.com/storage) for document storage, and a [Next.js](https://nextjs.org/) dashboard with real-time SSE streaming.

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

## ✅ What's Built (Phase 2) — RAG & Document Intelligence

### RAG Pipeline
- **Document Upload to GCS**: Upload PDF, DOCX, and TXT files via the dashboard. Files are stored in Google Cloud Storage with SHA-256 duplicate detection.
- **Ingestion Pipeline**: Automatic background processing: GCS Download → Text Extraction → Sentence-Boundary Chunking → Embedding via `nomic-embed-text` → Upsert to Qdrant.
- **Text Extraction**: Supports PDF (PdfPig), DOCX (OpenXml), and plain text/markdown files.
- **Semantic Chunking**: Sliding-window chunking with configurable token limits and overlap to preserve context across chunk boundaries.
- **Vector Search**: Qdrant gRPC-based similarity search retrieves the most relevant document chunks per conversation.
- **Context Injection**: Retrieved chunks are injected as a system message into the LLM prompt, grounding responses in uploaded document content with source file citations.

### Caching & Cost Optimization
- **Embedding Cache**: `IMemoryCache` caches query embeddings (30-min TTL) to avoid redundant Ollama calls on repeated queries.
- **Signed URL Cache**: GCS signed URLs are cached with dynamic TTL (`expiry - 5 min`) to eliminate repeated signing API calls.
- **Duplicate Document Detection**: SHA-256 hash check prevents re-uploading identical files within the same conversation (returns `409 Conflict`).
- **Configurable History Windowing**: Chat history window size and RAG retrieval chunk count are configurable via `appsettings.json`.

---

## 🏗️ Current Architecture

```
┌───────────────────────────────────────────────┐
│         Next.js 16 Dashboard (App Router)      │
│  Multi-chat UI · Document Upload · SSE Stream  │
└─────────────────────┬─────────────────────────┘
                      │ HTTP + SSE (with JWT Auth)
┌─────────────────────▼─────────────────────────┐
│            C# .NET 9 Web API (Controllers)     │
│         Microsoft Semantic Kernel (Kernel)     │
│  ┌──────────┬──────────┬──────────────────┐    │
│  │  Auth    │  Chat    │  Documents       │    │
│  │Controller│Controller│  Controller      │    │
│  └────┬─────┴────┬─────┴──────┬───────────┘    │
│       │          │            │                 │
│       │   ┌──────▼──────┐  ┌──▼──────────────┐  │
│       │   │ RAG Context │  │ Ingestion       │  │
│       │   │ Injection   │  │ Pipeline        │  │
│       │   └──────┬──────┘  │ Parse→Chunk→    │  │
│       │          │         │ Embed→Store     │  │
│       │          │         └──┬──────────────┘  │
└───────┼──────────┼────────────┼──────────────────┘
        │          │            │
   MongoDB     Qdrant     Google Cloud
  ┌─────────┐ ┌─────────┐  Storage
  │ Users   │ │ Vectors │ ┌─────────┐
  │ Convos  │ │ Chunks  │ │ PDFs    │
  │ Docs    │ │ (gRPC)  │ │ DOCX    │
  └─────────┘ └─────────┘ └─────────┘
        │          │
        └──────┬───┘
               │
         ┌─────▼─────┐
         │  Ollama   │
         │ llama3.2  │  ← Chat completion
         │ nomic-    │  ← Embeddings (768-dim)
         │ embed-text│
         └───────────┘
```

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | Next.js 16, React 19, TypeScript, Tailwind CSS v4 | Streaming chat UI with document upload |
| **Backend** | C# .NET 9, Semantic Kernel 1.77 | API orchestration, SSE streaming, RAG pipeline |
| **Database** | MongoDB (local Docker container) | Users, conversations, document metadata |
| **Vector DB** | Qdrant (local Docker container) | Semantic search over document embeddings |
| **Storage** | Google Cloud Storage | Document file storage (PDF, DOCX, TXT) |
| **LLM** | Ollama + llama3.2 | Free local chat inference |
| **Embeddings** | Ollama + nomic-embed-text | 768-dimensional text embeddings for RAG |
| **Caching** | IMemoryCache | Embedding cache, signed URL cache |

---

## 📁 Project Structure

```
VaultMind/
├── src/
│   ├── VaultMind.API/                # C# .NET Web API
│   │   ├── Program.cs                # Service registration & middleware
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs     # JWT signup, signin, token endpoint
│   │   │   ├── ChatController.cs     # POST /api/chat (RAG + streaming)
│   │   │   ├── DocumentsController.cs# Upload, list, delete documents
│   │   │   └── HealthController.cs   # GET /api/health
│   │   ├── Plugins/
│   │   │   └── UtilityPlugin.cs      # Native C# tools (clock, summarize)
│   │   ├── Prompts/
│   │   │   └── ChatPlugin/           # Dynamic prompt templates
│   │   ├── Services/
│   │   │   ├── JwtService.cs         # JWT token generator
│   │   │   ├── MongoDbContext.cs     # MongoDB connector context
│   │   │   ├── MongoRepository.cs    # Generic repository pattern
│   │   │   ├── SseService.cs         # SSE formatting & streaming logic
│   │   │   ├── GoogleStorageService.cs # GCS upload, download, signed URLs
│   │   │   ├── DocumentParserService.cs # PDF, DOCX, TXT text extraction
│   │   │   ├── ChunkingService.cs    # Sentence-boundary text chunking
│   │   │   ├── QdrantVectorStoreService.cs # Embedding, upsert, search
│   │   │   └── IngestionService.cs   # Orchestrates full ingestion pipeline
│   │   ├── Interfaces/
│   │   │   └── ...                   # API service contracts
│   │   └── appsettings.json          # MongoDB, Ollama, Qdrant, GCS config
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
- A Google Cloud service account JSON key (for GCS document storage)

### 1. Set Up Local Dependencies (Docker & Ollama)
```powershell
# Start MongoDB (port 27017)
docker run -d --name mongodb -p 27017:27017 mongo:latest

# Start Qdrant vector database (REST: 6333, gRPC: 6334)
docker run -d --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant

# Pull Ollama models
ollama pull llama3.2         # Chat completion
ollama pull nomic-embed-text # Embeddings (768-dim)
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
| **Qdrant over pgvector** | Purpose-built vector DB with native gRPC, per-collection isolation, and simple Docker deployment. |
| **nomic-embed-text** | Open-source 768-dim embedding model runs locally on Ollama — zero API cost for RAG. |
| **Conversation-scoped collections** | Each conversation gets its own Qdrant collection, providing natural document isolation between chats. |
| **SHA-256 dedup** | Hashing file content before upload prevents duplicate ingestion, saving GCS bandwidth and embedding compute. |
| **IMemoryCache** | Singleton in-memory cache for embeddings and signed URLs — simple, zero-dependency, thread-safe. |
| **File-Based Prompts** | Keeps prompt engineering clean and decoupled from C# compilation. |

---

## 🗺️ Roadmap — What's Next

VaultMind is being built in phases following a 6-month AI System Architect roadmap:

| Phase | Focus | Status |
|-------|-------|--------|
| **Phase 1** | Streaming chat app + auth + conversation memory + prompt templates | ✅ Complete |
| **Phase 2** | RAG pipeline — document upload, embedding, vector search, caching | ✅ Complete |
| **Phase 3** | C++ inference engine with ONNX Runtime, .NET interop via P/Invoke | 🔜 Next |
| **Phase 4** | Capstone — full platform with Redis caching, metrics dashboard, Docker | 📋 Planned |

### Current Limitations
- **CPU-only inference** — Ollama runs on CPU, so LLM responses on long prompts can take 30-120s. GPU acceleration would provide 10-50x speedup.
- **No OCR for scanned PDFs** — The PDF parser extracts embedded text only. Scanned/image-based PDFs return empty content.

---

## 📄 License

MIT
