# VaultMind.API

> C# .NET 9 backend service for VaultMind — handles chat orchestration and SSE streaming using Microsoft Semantic Kernel.

## What This Project Does

This is the backend API that sits between the Next.js frontend and the local Ollama LLM. It:

1. Receives a user message via `POST /api/chat`
2. Injects a system prompt (VaultMind identity) into a `ChatHistory`
3. Streams the LLM response back to the frontend token-by-token using **Server-Sent Events (SSE)**
4. Exposes a `GET /api/health` endpoint for frontend status monitoring

## Architecture

```
Incoming HTTP Request
        │
        ▼
   Program.cs          → Registers services, CORS, maps endpoints
        │
        ├── Endpoints/
        │   ├── ChatEndpoints.cs     → POST /api/chat (SSE streaming)
        │   └── HealthEndpoints.cs   → GET /api/health
        │
        ├── Services/
        │   └── SseService.cs        → Formats IAsyncEnumerable<string> into SSE
        │
        └── Interfaces/
            └── ISseService.cs       → Service contract for SSE
```

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `Microsoft.SemanticKernel` | LLM orchestration, chat completion |
| `Microsoft.SemanticKernel.Connectors.OpenAI` | OpenAI-compatible connector (works with Ollama) |

## Configuration

All config lives in `appsettings.json`:

```json
{
  "OpenAI": {
    "ModelId": "phi3",
    "ApiKey": "ollama",
    "Endpoint": "http://localhost:11434/v1"
  }
}
```

- **ModelId** — The Ollama model name (run `ollama list` to see available models)
- **ApiKey** — Set to `"ollama"` (Ollama doesn't require a real key, but the SDK needs a non-empty value)
- **Endpoint** — Ollama's OpenAI-compatible API endpoint

## Running

```bash
# Make sure Ollama is running with phi3 pulled
ollama pull phi3

# Start the API
dotnet run
```

The API starts on `http://localhost:5139`. CORS is configured to allow requests from `http://localhost:3000` (the frontend).

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/chat` | Streams an AI response via SSE. Body: `{ "message": "your question" }` |
| `GET` | `/api/health` | Returns service status, name, and timestamp |
