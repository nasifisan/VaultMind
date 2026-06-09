# VaultMind Architecture

## System Overview

```
┌──────────────────────────────────────────────────────────┐
│             Next.js Dashboard (App Router)                │
│         (Streaming UI, Charts, Document Upload)           │
└────────────────────────┬─────────────────────────────────┘
                         │ SSE / WebSocket
┌────────────────────────▼─────────────────────────────────┐
│                 C# .NET 8 Web API                         │
│            Microsoft Semantic Kernel                      │
│     ┌──────────┬──────────────┬──────────────┐           │
│     │  Chat    │  RAG         │  Caching     │           │
│     │  Engine  │  Pipeline    │  Layer       │           │
│     └────┬─────┴──────┬───────┴──────┬───────┘           │
└──────────┼────────────┼──────────────┼───────────────────┘
           │            │              │
     ┌─────▼─────┐ ┌───▼────┐  ┌─────▼─────┐
     │   C++     │ │ Qdrant │  │   Redis   │
     │  Engine   │ │ Vector │  │  Semantic  │
     │  (ONNX)   │ │   DB   │  │   Cache   │
     └───────────┘ └────────┘  └───────────┘
```

## Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Frontend | Next.js 15 (App Router) | SSR, streaming UI, real-time dashboards |
| Backend | C# .NET 8, Semantic Kernel | API, orchestration, RAG pipeline |
| Inference | C++20, ONNX Runtime | Local model execution, quantization |
| Vector DB | Qdrant | Semantic document search |
| Cache | Redis | Semantic caching, cost reduction |
| DevOps | Docker, Docker Compose | Containerized deployment |

## Data Flow

1. User uploads documents → .NET API → Chunking → Embedding → Qdrant
2. User asks a question → .NET API → Semantic search (Qdrant) → Context assembly
3. Context + question → C++ engine (ONNX inference) → Streamed response → React UI
4. Redis caches semantically similar queries to avoid redundant inference

## Interop: .NET ↔ C++

The C++ engine exposes a C API (`engine.h`) callable via P/Invoke from .NET:
- `vm_engine_init()` — Load ONNX model
- `vm_engine_infer()` — Run inference on text input
- `vm_engine_shutdown()` — Cleanup resources
