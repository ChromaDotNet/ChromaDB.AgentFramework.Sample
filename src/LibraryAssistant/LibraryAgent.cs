using ChromaDB.VectorData;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace LibraryAssistant;

/// <summary>
/// An agent that answers from the library handbook stored in Chroma, and remembers in Chroma what each reader tells it.
/// </summary>
public sealed class LibraryAgent : IDisposable
{
    public const string HandbookCollectionName = "library-handbook";
    public const string MemoryCollectionName = "library-memory";

    private const string Instructions =
        "You are the assistant of the Riverside Community Library. Answer from the handbook passages and from what the reader told you before. " +
        "If they do not contain the answer, say that you do not know. Answer in at most three sentences.";

    private readonly ChatHistoryMemoryProvider _memory;

    private LibraryAgent(AIAgent agent, ChatHistoryMemoryProvider memory)
    {
        Agent = agent;
        _memory = memory;
    }

    public AIAgent Agent { get; }

    /// <summary>
    /// Stores the handbook in Chroma and creates the agent for one reader.
    /// </summary>
    /// <param name="chatClient">The chat model.</param>
    /// <param name="vectorStore">The Chroma vector store, with the embedding generator of the embedding model.</param>
    /// <param name="embeddingDimensions">The number of dimensions of the embeddings.</param>
    /// <param name="readerId">The reader whose memories the agent stores and searches.</param>
    public static async Task<LibraryAgent> CreateAsync(IChatClient chatClient, ChromaVectorStore vectorStore, int embeddingDimensions, string readerId, CancellationToken cancellationToken = default)
    {
        var handbook = vectorStore.GetCollection<string, HandbookNote>(HandbookCollectionName, HandbookNote.Definition(embeddingDimensions));
        await handbook.EnsureCollectionExistsAsync(cancellationToken);
        await handbook.UpsertAsync(Handbook.Notes, cancellationToken);

        // Before each call to the model, the passages nearest to the question are searched in Chroma and added to the context.
        var handbookSearch = new TextSearchProvider(async (question, token) =>
        {
            var passages = new List<TextSearchProvider.TextSearchResult>();
            await foreach (var result in handbook.SearchAsync(question, top: 3, cancellationToken: token))
            {
                passages.Add(new() { SourceName = result.Record.Topic, Text = result.Record.Text });
            }

            return passages;
        });

        // The messages of the conversations are stored in Chroma, and the ones of the same reader are searched in later sessions.
        var memory = new ChatHistoryMemoryProvider(vectorStore, MemoryCollectionName, embeddingDimensions,
            _ => new ChatHistoryMemoryProvider.State(new ChatHistoryMemoryProviderScope { UserId = readerId }));

        var agent = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "LibraryAssistant",
            ChatOptions = new ChatOptions { Instructions = Instructions },
            AIContextProviders = [handbookSearch, memory],
        });

        return new LibraryAgent(agent, memory);
    }

    public void Dispose() => _memory.Dispose();
}
