[![ChromaDotNet](https://raw.githubusercontent.com/ChromaDotNet/.github/main/assets/logo-64.png)](https://chromadotnet.org)

# ChromaDB.AgentFramework.Sample

[Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) samples that use [Chroma](https://www.trychroma.com/) as the vector store through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).

They follow the form of the [Agent Framework .NET samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents). Chroma takes the place of the vector store that the original sample uses. Each sample also comes in a variant with models that run locally in Ollama.

> This is a community project. It is not affiliated with or endorsed by Chroma.

Website: [chromadotnet.org](https://chromadotnet.org)

## Samples

| Sample | Description |
|---|---|
| [Memory with Chroma](./samples/AgentWithMemory_Step10_MemoryUsingChroma/) | Persists chat history in Chroma with `ChatHistoryMemoryProvider` and recalls it in a new session. Uses Microsoft Foundry models. Follows the form of [Memory with Azure Cosmos DB for NoSQL](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithMemory/AgentWithMemory_Step08_MemoryUsingCosmosNoSql). |
| [Memory with Chroma and Ollama](./samples/AgentWithMemory_Step10_MemoryUsingChroma_Ollama/) | The same sample with models that run locally in Ollama. |
| [RAG with Chroma](./samples/AgentWithRAG_Step06_ChromaRAG/) | Answers from documentation stored in Chroma with a custom schema, through `TextSearchProvider`. Uses Microsoft Foundry models. Follows the form of [RAG with Vector Store and custom schema](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithRAG/AgentWithRAG_Step02_CustomVectorStoreRAG). |
| [RAG with Chroma and Ollama](./samples/AgentWithRAG_Step06_ChromaRAG_Ollama/) | The same sample with models that run locally in Ollama. |
| [RAG with Chroma and the TextSearchStore](./samples/AgentWithRAG_Step07_ChromaBasicTextRAG/) | Answers from documents stored in Chroma, through `TextSearchProvider`. The documents are stored by the `TextSearchStore` from the Agent Framework sample, which writes and reads them as dictionaries. Uses Microsoft Foundry models. Follows the form of [Basic Text RAG](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/02-agents/AgentWithRAG/AgentWithRAG_Step01_BasicTextRAG). |
| [RAG with Chroma, the TextSearchStore and Ollama](./samples/AgentWithRAG_Step07_ChromaBasicTextRAG_Ollama/) | The same sample with models that run locally in Ollama. |

The Microsoft Foundry samples need a Foundry project and `az login`. Each sample README lists what it needs.

The Ollama samples need only the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and Docker. The [compose.yaml](./compose.yaml) file runs Chroma and Ollama, and downloads the models.

```bash
docker compose up -d
dotnet run --project samples/AgentWithMemory_Step10_MemoryUsingChroma_Ollama
```

## Tests

```bash
dotnet test
```

GitHub Actions runs the tests on every push to `main` and on every pull request to `main`.

The tests run each sample's scenario with the same configuration as the sample. Chroma runs in a container started with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers). The tests need Docker, but no model. The embeddings come from word hashes, and the chat model only records what the agent sends to it.

What the tests check:

- Memory: the user's preference reaches the model in a new session, and another user's messages do not. Each session is stored under its own session id.
- RAG: the chunk that answers the question reaches the model, and the search results are not stored in the chat history. A follow-up question is searched with the recent messages. The chunks are stored with their source.
- RAG with the TextSearchStore: the document that answers each question reaches the model, and the search results are not stored in the chat history. The documents are stored with their source.

The `TextSearchStore` in [AgentWithRAG_Step07_ChromaBasicTextRAG/TextSearchStore](./samples/AgentWithRAG_Step07_ChromaBasicTextRAG/TextSearchStore/) is copied unchanged from the [Agent Framework repository](https://github.com/microsoft/agent-framework) (MIT) and keeps its copyright notice.

The sample programs are adaptations of the Agent Framework samples linked in the table above. They are under the same license and carry the same copyright notice.
