# Agent with Memory Using Chroma and Ollama

This sample is [Agent with Memory Using Chroma](../AgentWithMemory_Step10_MemoryUsingChroma/) with models that run locally in [Ollama](https://ollama.com/) instead of Microsoft Foundry.

It persists chat history in [Chroma](https://www.trychroma.com/) and recalls relevant messages in a new agent session. It uses `ChatHistoryMemoryProvider` with `ChromaVectorStore` from [CommunityToolkit.VectorData.Chroma](https://www.nuget.org/packages/CommunityToolkit.VectorData.Chroma).

## Features Demonstrated

- Using a chat model and an embedding model that run locally in Ollama
- Storing chat messages in a Chroma vector store
- Creating the chat-history collection when it does not exist
- Recalling relevant chat history across agent sessions

## Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Docker, to run Chroma and Ollama with the [compose file](../../compose.yaml) at the root of the repository:

   ```bash
   docker compose up -d
   ```

   The first run downloads `qwen2.5:3b` and `nomic-embed-text` (about 2 GB). To see the progress, run `docker compose logs -f ollama-models`.

## Configuration

The defaults match the compose file. To change them, set the following environment variables:

| Variable | Description | Default |
|---|---|---|
| `OLLAMA_ENDPOINT` | Ollama endpoint | `http://localhost:11434` |
| `OLLAMA_MODEL_NAME` | Chat model | `qwen2.5:3b` |
| `OLLAMA_EMBEDDING_MODEL_NAME` | Embedding model | `nomic-embed-text` |
| `OLLAMA_EMBEDDING_DIMENSIONS` | Number of dimensions produced by the embedding model | `768` |
| `CHROMA_ENDPOINT` | Chroma server endpoint | `http://localhost:8000` |

## Run the Sample

```bash
dotnet run
```

The first session stores the user's preference for pirate jokes. The second session uses a different `AgentSession` but the same per-run user search scope. This lets the agent retrieve that preference from Chroma without recalling data from earlier sample runs.

The chat history goes in its own collection, `chathistory-local`, because the local embedding model has fewer dimensions than the Foundry one.
