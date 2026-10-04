# ChromaDB.AgentFramework.Sample

A [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/) agent that uses [Chroma](https://www.trychroma.com/) as its vector store, through [ChromaDotNet.VectorData](https://www.nuget.org/packages/ChromaDotNet.VectorData).

> This is a community project. It is not affiliated with or endorsed by Chroma.

The agent is the assistant of the Riverside Community Library, a library that exists only in this sample, so the model can answer only from what it finds in Chroma:

- **Handbook search.** The rules of the library are stored in a Chroma collection. Before each call to the model, a [`TextSearchProvider`](https://learn.microsoft.com/agent-framework/agents/rag) searches the passages nearest to the question and adds them to the context.
- **Memory.** A [`ChatHistoryMemoryProvider`](https://learn.microsoft.com/agent-framework/concepts/agents/conversations/chat-history-memory-provider) stores what each reader says in another Chroma collection, and searches it in later sessions of the same reader.

The models run locally with [Ollama](https://ollama.com/): `qwen2.5:3b` for the chat and `all-minilm` for the embeddings. No API key is needed.

## Run it

You need Docker and the .NET 10 SDK.

```bash
docker compose up -d
dotnet run --project src/LibraryAssistant
```

The first `docker compose up` downloads the two models, about 2 GB; `docker compose logs -f ollama-models` shows the progress. Each run of the sample starts from an empty memory. The chat model answers at temperature 0 and the images are pinned, so on the same machine every run gives these same answers.

An actual run:

```text
First session
> Hi, I'm Ada. I love science fiction novels.
Hello Ada! That's great that you love science fiction novels. The science fiction book club meets on the first Thursday of every month at 18:30 in room B. You're welcome to join and there's no need to book.
> Can I bring my dog to the library?
No, only assistance dogs are allowed in the library. Other dogs should wait at the leash hooks next to the entrance, where there is a water bowl. The library is open Monday to Friday from 9:00 to 19:00 and on Saturday from 10:00 to 14:00.
> Until what time is the library open on Saturday?
The library is open on Saturday from 10:00 to 14:00.

Second session
> Is there an event at the library that I might enjoy?
The Riverside Community Library hosts a science fiction book club on the first Thursday of every month at 18:30 in room B. This might be something you enjoy, given your love for science fiction novels.
```

The second session has no chat history: the agent knows that Ada likes science fiction from the memory in Chroma, and about the book club from the handbook in Chroma.

## Settings

| Variable | Default |
|---|---|
| `CHROMA_URI` | `http://localhost:8000` |
| `OLLAMA_URI` | `http://localhost:11434` |
| `CHAT_MODEL` | `qwen2.5:3b` |
| `EMBEDDING_MODEL` | `all-minilm` |
| `EMBEDDING_DIMENSIONS` | `384`, the dimensions of `all-minilm` |

## Tests

```bash
dotnet test
```

GitHub Actions runs them on every push.

The tests start Chroma in a container with [ChromaDotNet.Testcontainers](https://www.nuget.org/packages/ChromaDotNet.Testcontainers), so they need Docker, but no model: the embeddings come from word hashes and the chat model only records what the agent sends to it. They check that the handbook is stored in Chroma, that the passage that answers the question reaches the model, that a reader's words reach the model in a later session, that the answers of the agent are not remembered, that one reader's memories do not reach another reader, and that the agent asks the model for temperature 0.
