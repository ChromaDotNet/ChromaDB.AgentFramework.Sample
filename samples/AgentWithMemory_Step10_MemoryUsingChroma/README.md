# Agent with Memory Using Chroma

This sample persists chat history in [Chroma](https://www.trychroma.com/) and recalls relevant messages in a new agent session. It uses `ChatHistoryMemoryProvider` with `ChromaVectorStore` from [CommunityToolkit.VectorData.Chroma](https://www.nuget.org/packages/CommunityToolkit.VectorData.Chroma).

## Features Demonstrated

- Authenticating to Microsoft Foundry with `DefaultAzureCredential`
- Storing chat messages in a Chroma vector store
- Creating the chat-history collection when it does not exist
- Recalling relevant chat history across agent sessions

## Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. A Microsoft Foundry project with:
   - A chat model deployment (the default is `gpt-5.4-mini`)
   - A `text-embedding-3-large` deployment with 3,072 dimensions
3. A running Chroma server, version 1.5.0 or later. To start the instance defined in the [compose file](../../compose.yaml), run `docker compose up -d chroma` at the root of the repository. Or you can run one with Docker:

   ```bash
   docker run -d --name chroma -p 8000:8000 chromadb/chroma:1.5.9
   ```

4. Azure CLI authentication (`az login`) with an identity that has the Foundry User role on the Foundry resource

The Foundry project endpoint does not serve embeddings. The sample generates them through the endpoint of the Foundry resource that hosts the project, using the same credential.

## Configuration

Set the following environment variables:

| Variable | Description | Default |
|---|---|---|
| `FOUNDRY_PROJECT_ENDPOINT` | Microsoft Foundry project endpoint | *(required)* |
| `FOUNDRY_MODEL` | Chat model deployment name | `gpt-5.4-mini` |
| `FOUNDRY_EMBEDDING_MODEL` | Embedding model deployment name | `text-embedding-3-large` |
| `FOUNDRY_EMBEDDING_DIMENSIONS` | Number of dimensions produced by the embedding deployment | `3072` |
| `CHROMA_ENDPOINT` | Chroma server endpoint | `http://localhost:8000` |

## Run the Sample

```bash
dotnet run
```

The first session stores the user's preference for pirate jokes. The second session uses a different `AgentSession` but the same per-run user search scope. This lets the agent retrieve that preference from Chroma without recalling data from earlier sample runs.
