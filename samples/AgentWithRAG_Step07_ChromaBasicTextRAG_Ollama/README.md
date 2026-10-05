# Agent Framework Retrieval Augmented Generation (RAG) with Chroma, the TextSearchStore and Ollama

This sample is [RAG with Chroma and the TextSearchStore](../AgentWithRAG_Step07_ChromaBasicTextRAG/) with models that run locally in [Ollama](https://ollama.com/) instead of Microsoft Foundry. It demonstrates how to create and run an agent that uses Retrieval Augmented Generation (RAG) with [Chroma](https://www.trychroma.com/) as the vector store, through `ChromaVectorStore` from [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData), with the documents stored by the `TextSearchStore` of that sample.

## Prerequisites

- .NET 10 SDK or later
- Docker, to run Chroma and Ollama with the [compose file](../../compose.yaml) at the root of the repository:

```powershell
docker compose up -d
```

The first run downloads `qwen2.5:3b` and `nomic-embed-text`, about 2 GB; `docker compose logs -f ollama-models` shows the progress.

## Running the sample from the console

The defaults match the compose file. To change them, set the following environment variables:

```powershell
$env:OLLAMA_ENDPOINT="http://localhost:11434"  # Optional, defaults to http://localhost:11434
$env:OLLAMA_MODEL_NAME="qwen2.5:3b"  # Optional, defaults to qwen2.5:3b
$env:OLLAMA_EMBEDDING_MODEL_NAME="nomic-embed-text"  # Optional, defaults to nomic-embed-text
$env:OLLAMA_EMBEDDING_DIMENSIONS="768"  # Optional, defaults to 768, the dimensions of nomic-embed-text
$env:CHROMA_ENDPOINT="http://localhost:8000"  # Optional, defaults to http://localhost:8000
```

Execute the following command to build and run the sample:

```powershell
dotnet run
```

The documents go in a collection of their own, `product-and-policy-info-local`, since the local embedding model has fewer dimensions than the Foundry one.
