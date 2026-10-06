# Agent Framework Retrieval Augmented Generation (RAG) with Chroma and Ollama

This sample is [RAG with Chroma and a custom schema](../AgentWithRAG_Step06_ChromaRAG/) with models that run locally in [Ollama](https://ollama.com/) instead of Microsoft Foundry.

It demonstrates how to create and run an agent that uses Retrieval Augmented Generation (RAG) with [Chroma](https://www.trychroma.com/) as the vector store. It connects to Chroma through `ChromaVectorStore` from [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).
It also uses a custom schema for the documents.

## Prerequisites

- .NET 10 SDK or later
- Docker, to run Chroma and Ollama with the [compose file](../../compose.yaml) at the root of the repository:

```powershell
docker compose up -d
```

The first run downloads `qwen2.5:3b` and `nomic-embed-text` (about 2 GB). To see the progress, run `docker compose logs -f ollama-models`.

**Note**: The search text includes the recent messages, so it can be long. The `nomic-embed-text` model embeds up to 8,192 tokens, while smaller embedding models would cut off the end of the text, which is the latest question.

The sample also raises the Ollama context window, because five chunks of documentation and the chat history do not fit in the default one. A larger chat model than `qwen2.5:3b` gives better answers.

## Running the sample from the console

The defaults match the compose file. To change them, set the following environment variables:

```powershell
$env:OLLAMA_ENDPOINT="http://localhost:11434"  # Optional, defaults to http://localhost:11434
$env:OLLAMA_MODEL_NAME="qwen2.5:3b"  # Optional, defaults to qwen2.5:3b
$env:OLLAMA_EMBEDDING_MODEL_NAME="nomic-embed-text"  # Optional, defaults to nomic-embed-text, which has 768 dimensions
$env:CHROMA_ENDPOINT="http://localhost:8000"  # Optional, defaults to http://localhost:8000
```

Execute the following command to build and run the sample:

```powershell
dotnet run
```

The documents go in their own collection, `documentation-local`, because the local embedding model has fewer dimensions than the Foundry one.
