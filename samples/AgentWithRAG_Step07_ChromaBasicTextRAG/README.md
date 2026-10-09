# Agent Framework Retrieval Augmented Generation (RAG) with Chroma and the TextSearchStore

This sample demonstrates how to create and run an agent that uses Retrieval Augmented Generation (RAG) with [Chroma](https://www.trychroma.com/) as the vector store. It connects to Chroma through `ChromaVectorStore` from [CommunityToolkit.VectorData.Chroma](https://www.nuget.org/packages/CommunityToolkit.VectorData.Chroma).
The documents are stored by the `TextSearchStore` from the [Agent Framework sample](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithRAG/AgentWithRAG_Step01_BasicTextRAG), copied here unchanged. It is a sample store implementation that hardcodes a storage schema. It writes and reads the records as dictionaries.

## Prerequisites

- .NET 10 SDK or later
- A Microsoft Foundry project with:
  - A chat model deployment (the default is `gpt-5.4-mini`)
  - A `text-embedding-3-large` deployment with 3,072 dimensions
- Azure CLI installed and authenticated (`az login`) with an identity that has the Foundry User role on the Foundry resource
- A running Chroma server, version 1.5.0 or later. To start the instance defined in the [compose file](../../compose.yaml), run `docker compose up -d chroma` at the root of the repository. Or you can run a local instance using Docker:

```powershell
docker run -d --name chroma -p 8000:8000 chromadb/chroma:1.5.9
```

**Note**: The Foundry project endpoint does not serve embeddings. The sample generates them through the endpoint of the Foundry resource that hosts the project, using the same credential.

**Note**: The sample keeps the chat history in an `InMemoryChatHistoryProvider`, so the `TextSearchProvider` search results are not stored with it. Otherwise, Microsoft Foundry would store the responses and manage the conversation itself. That cannot be combined with a `ChatHistoryProvider`, so the sample turns off response storage.

## Running the sample from the console

Set the following environment variables:

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT="https://your-resource.services.ai.azure.com/api/projects/your-project" # Replace with your Microsoft Foundry project endpoint
$env:FOUNDRY_MODEL="gpt-5.4-mini"  # Optional, defaults to gpt-5.4-mini
$env:FOUNDRY_EMBEDDING_MODEL="text-embedding-3-large"  # Optional, defaults to text-embedding-3-large; the sample expects its 3,072 dimensions
$env:CHROMA_ENDPOINT="http://localhost:8000"  # Optional, defaults to http://localhost:8000
```

Execute the following command to build and run the sample:

```powershell
dotnet run
```

The sample stores three Contoso Outdoors documents in Chroma and asks three questions about them.

Before each question, the search returns only the top document. This shows that each question finds the document that answers it. The agent cites the source of that document.
