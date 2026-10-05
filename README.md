# ChromaDB.AgentFramework.Sample

[Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) samples that use [Chroma](https://www.trychroma.com/) as the vector store, through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData). They follow the form of the [.NET samples of Agent Framework](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents), with Chroma in place of the vector store of the original sample, and each one also comes with models that run locally in Ollama.

> This is a community project. It is not affiliated with or endorsed by Chroma.

## Samples

| Sample | Description |
|---|---|
| [Memory with Chroma](./samples/AgentWithMemory_Step10_MemoryUsingChroma/) | Persists chat history in Chroma with `ChatHistoryMemoryProvider` and recalls it in a new session, with Microsoft Foundry models. In the form of [Memory with Azure Cosmos DB for NoSQL](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithMemory/AgentWithMemory_Step08_MemoryUsingCosmosNoSql). |
| [Memory with Chroma and Ollama](./samples/AgentWithMemory_Step10_MemoryUsingChroma_Ollama/) | The same sample with models that run locally in Ollama. |
| [RAG with Chroma](./samples/AgentWithRAG_Step06_ChromaRAG/) | Answers from documentation stored in Chroma with a custom schema, through `TextSearchProvider`, with Microsoft Foundry models. In the form of [RAG with Vector Store and custom schema](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithRAG/AgentWithRAG_Step02_CustomVectorStoreRAG). |
| [RAG with Chroma and Ollama](./samples/AgentWithRAG_Step06_ChromaRAG_Ollama/) | The same sample with models that run locally in Ollama. |

The Microsoft Foundry samples need a Foundry project and `az login`; each README lists what it needs. The Ollama samples need only Docker: [compose.yaml](./compose.yaml) runs Chroma and Ollama, and downloads the models.

```bash
docker compose up -d
dotnet run --project samples/AgentWithMemory_Step10_MemoryUsingChroma_Ollama
```

## Tests

```bash
dotnet test
```

GitHub Actions runs them on every push.

The tests run the scenario of each sample, with the same configuration, against Chroma in a container started with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers). They need Docker, but no model: the embeddings come from word hashes and the chat model only records what the agent sends to it.

- Memory: the preference of the user reaches the model in a new session, the messages of another user do not, and each session is stored under its own session id.
- RAG: the chunk that answers the question reaches the model, the search results are not stored in the chat history, a follow-up question is searched with the recent messages, and the chunks are stored with their source.
