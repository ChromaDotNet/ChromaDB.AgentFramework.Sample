# Agent Framework Retrieval Augmented Generation (RAG) with Chroma and a custom schema

This sample demonstrates how to create and run an agent that uses Retrieval Augmented Generation (RAG) with [Chroma](https://www.trychroma.com/) as the vector store, through `ChromaVectorStore` from [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).
It also uses a custom schema for the documents stored in the vector store.

## Prerequisites

- .NET 10 SDK or later
- A Microsoft Foundry project with:
  - A chat model deployment (the default is `gpt-5.4-mini`)
  - A `text-embedding-3-large` deployment with 3,072 dimensions
- Azure CLI installed and authenticated (`az login`) with an identity that has the Foundry User role on the Foundry resource
- A running Chroma server, 1.5.0 or later: `docker compose up -d chroma` at the root of the repository starts the one of the [compose file](../../compose.yaml), or you can run a local instance using Docker:

```powershell
docker run -d --name chroma -p 8000:8000 chromadb/chroma:1.5.9
```

**Note**: The Foundry project endpoint does not serve embeddings, so the sample generates them through the endpoint of the Foundry resource that hosts the project, with the same credential.

**Note**: The sample keeps the chat history in an `InMemoryChatHistoryProvider`, so that the search results of the `TextSearchProvider` are not stored with it. Microsoft Foundry would otherwise store the responses and manage the conversation itself, which cannot be combined with a `ChatHistoryProvider`, so the sample turns off the storage of the responses.

## Running the sample from the console

Set the following environment variables:

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT="https://your-resource.services.ai.azure.com/api/projects/your-project" # Replace with your Microsoft Foundry project endpoint
$env:FOUNDRY_MODEL="gpt-5.4-mini"  # Optional, defaults to gpt-5.4-mini
$env:FOUNDRY_EMBEDDING_MODEL="text-embedding-3-large"  # Optional, defaults to text-embedding-3-large; the sample expects its 3,072 dimensions
$env:CHROMA_ENDPOINT="http://localhost:8000"  # Optional, defaults to http://localhost:8000
```

Execute the following command to build the sample:

```powershell
dotnet build
```

Execute the following command to run the sample:

```powershell
dotnet run --no-build
```

Or just build and run in one step:

```powershell
dotnet run
```

The sample downloads two documents of the Agent Framework documentation, stores them in Chroma in chunks, and asks four questions about them. The second question, "and in Agent Framework?", only makes sense after the first one: the search includes the recent messages, so it still finds the right chunks.
