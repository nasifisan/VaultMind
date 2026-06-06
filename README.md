# 🔒🧠 VaultMind

> A high-performance document intelligence platform powered by local C++ inference, .NET orchestration, and real-time streaming analytics.

**VaultMind** ingests documents, understands them semantically, and answers questions with blazing-fast local inference. The core engine runs quantized ONNX models in C++ at millisecond latency, orchestrated through a C# .NET Semantic Kernel backend with RAG and semantic caching. A Next.js dashboard provides real-time visibility into inference performance, cache efficiency, and document processing — all containerized with Docker for one-command deployment.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│            Next.js Dashboard (App Router)             │
│        Streaming UI · Charts · Document Upload       │
└──────────────────────┬──────────────────────────────┘
                       │ SSE / WebSocket
┌──────────────────────▼──────────────────────────────┐
│              C# .NET 8 Web API                       │
│           Microsoft Semantic Kernel                  │
│    ┌──────────┬─────────────┬──────────────┐        │
│    │   Chat   │     RAG     │   Caching    │        │
│    │  Engine  │  Pipeline   │    Layer     │        │
│    └────┬─────┴──────┬──────┴──────┬───────┘        │
└─────────┼────────────┼─────────────┼────────────────┘
          │            │             │
    ┌─────▼─────┐ ┌───▼────┐  ┌────▼─────┐
    │   C++20   │ │ Qdrant │  │  Redis   │
    │  Engine   │ │ Vector │  │ Semantic │
    │  (ONNX)   │ │   DB   │  │  Cache   │
    └───────────┘ └────────┘  └──────────┘
```

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend** | Next.js 15 (App Router) | SSR, streaming UI, real-time dashboards |
| **Backend** | C# .NET 8, Semantic Kernel | API, orchestration, RAG pipeline |
| **Inference** | C++20, ONNX Runtime | Local model execution, quantization |
| **Vector DB** | Qdrant | Semantic document search |
| **Cache** | Redis | Semantic caching, cost reduction |
| **DevOps** | Docker, Docker Compose | Containerized deployment |

## 📁 Project Structure

```
VaultMind/
├── src/
│   ├── VaultMind.API/            # C# .NET Web API + Semantic Kernel
│   ├── VaultMind.Engine/         # C++20 inference engine (ONNX Runtime)
│   └── vaultmind-dashboard/     # Next.js frontend (App Router)
├── infra/
│   ├── docker-compose.yml       # One-command deployment
│   ├── Dockerfile.api           # .NET multi-stage build
│   └── Dockerfile.dashboard    # Next.js standalone build
├── docs/
│   └── architecture.md          # Detailed architecture docs
├── .gitignore
└── README.md
```

## 🚀 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dot.net/download)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run Locally (Development)

**Backend:**
```bash
cd src/VaultMind.API
dotnet run
```

**Frontend:**
```bash
cd src/vaultmind-dashboard
npm install
npm run dev
```

### Run with Docker
```bash
cd infra
docker compose up --build
```

## 📄 License

MIT
