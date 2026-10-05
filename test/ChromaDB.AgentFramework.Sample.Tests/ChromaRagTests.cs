using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChromaDB.AgentFramework.Sample.Tests;

/// <summary>
/// The scenario of AgentWithRAG_Step06_ChromaRAG, with the same schema, TextSearchProvider options and chat history filter,
/// against Chroma in a container, with documents written here and without a model.
/// </summary>
public sealed class ChromaRagTests(ChromaFixture chroma) : IClassFixture<ChromaFixture>
{
    private const string AnswerToThreads = "In Semantic Kernel you create a ChatHistoryAgentThread object yourself before invoking the agent.";
    private const string AnswerToSessions = "In Agent Framework the agent creates the session: call CreateSessionAsync and pass the session to RunAsync.";

    private static readonly string[] s_documents =
    [
        AnswerToThreads,
        AnswerToSessions,
        "Tools are registered on the agent as functions that the model can call.",
        "Telemetry is exported with OpenTelemetry traces, metrics and logs.",
        "Workflows connect executors with edges and run them in supersteps.",
        "Declarative agents are defined in YAML files and loaded at startup.",
        "Middleware can inspect and change the messages of every agent run.",
        "Human in the loop approvals pause a run until a person answers.",
        "Model Context Protocol servers expose tools to the agent over stdio or HTTP.",
    ];

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Gives_the_model_the_chunk_that_answers_the_question()
    {
        var model = new RecordingChatClient();
        var agent = await CreateAgentAsync(model, []);

        await agent.RunAsync("How do I create a ChatHistoryAgentThread object in Semantic Kernel?", await agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);

        Assert.Contains(AnswerToThreads, model.LastMessagesText);
    }

    [Fact]
    public async Task Does_not_store_the_search_results_in_the_chat_history()
    {
        var model = new RecordingChatClient();
        var agent = await CreateAgentAsync(model, []);
        var session = await agent.CreateSessionAsync(_cancellationToken);

        await agent.RunAsync("How do I create a ChatHistoryAgentThread object in Semantic Kernel?", session, cancellationToken: _cancellationToken);
        await agent.RunAsync("How does the agent create the session in Agent Framework?", session, cancellationToken: _cancellationToken);

        // The second request has the earlier question from the chat history, and the search results of the second question only.
        Assert.Contains("How do I create a ChatHistoryAgentThread object in Semantic Kernel?", model.LastMessagesText);
        Assert.Single(model.LastMessages, message => message.Text.Contains("## Additional Context"));
    }

    [Fact]
    public async Task Searches_with_the_recent_messages_for_a_follow_up_question()
    {
        List<string> searches = [];
        var agent = await CreateAgentAsync(new RecordingChatClient(), searches);
        var session = await agent.CreateSessionAsync(_cancellationToken);

        await agent.RunAsync("How do I create a thread in Semantic Kernel?", session, cancellationToken: _cancellationToken);
        await agent.RunAsync("and in Agent Framework?", session, cancellationToken: _cancellationToken);

        Assert.Equal(2, searches.Count);
        Assert.Contains("How do I create a thread in Semantic Kernel?", searches[1]);
        Assert.Contains("and in Agent Framework?", searches[1]);
    }

    [Fact]
    public async Task Stores_the_chunks_in_Chroma_with_their_source()
    {
        await CreateAgentAsync(new RecordingChatClient(), []);

        var collection = chroma.ChromaClient.GetCollectionClient(await chroma.ChromaClient.GetCollectionAsync(CollectionName, cancellationToken: _cancellationToken));
        var records = await collection.GetAsync(include: ChromaDB.Client.ChromaGetInclude.Metadatas, cancellationToken: _cancellationToken);

        Assert.Equal(s_documents.Length, records.Count);
        Assert.All(records, record => Assert.Equal("Agent Framework notes", record.Metadata!["SourceName"]));
        Assert.All(records, record => Assert.True(Guid.TryParse(record.Id, out _)));
    }

    private const string CollectionName = "documentation";

    // The configuration of the sample: a custom schema, the search before each call with the recent messages,
    // and a chat history that does not store the search results.
    private async Task<AIAgent> CreateAgentAsync(IChatClient model, List<string> searches)
    {
        var collection = chroma.VectorStore.GetCollection<Guid, DocumentationChunk>(CollectionName);
        await collection.EnsureCollectionDeletedAsync(_cancellationToken);
        await collection.EnsureCollectionExistsAsync(_cancellationToken);
        await collection.UpsertAsync(
            s_documents.Select(text => new DocumentationChunk { Key = Guid.NewGuid(), SourceLink = "https://example.org/notes", SourceName = "Agent Framework notes", Text = text }),
            _cancellationToken);

        async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(string text, CancellationToken ct)
        {
            searches.Add(text);
            List<TextSearchProvider.TextSearchResult> results = [];
            await foreach (var result in collection.SearchAsync(text, 5, cancellationToken: ct))
            {
                results.Add(new TextSearchProvider.TextSearchResult { SourceName = result.Record.SourceName, SourceLink = result.Record.SourceLink, Text = result.Record.Text, RawRepresentation = result });
            }
            return results;
        }

        return model.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new() { Instructions = "You are a helpful support specialist for the Microsoft Agent Framework." },
            AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, new TextSearchProviderOptions
            {
                SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
                RecentMessageMemoryLimit = 5
            })],
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
            {
                StorageInputRequestMessageFilter = msgs => msgs.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory && m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider)
            })
        });
    }

    internal sealed class DocumentationChunk
    {
        [VectorStoreKey]
        public Guid Key { get; set; }
        [VectorStoreData]
        public string SourceLink { get; set; } = string.Empty;
        [VectorStoreData]
        public string SourceName { get; set; } = string.Empty;
        [VectorStoreData]
        public string Text { get; set; } = string.Empty;
        [VectorStoreVector(dimensions: ChromaFixture.EmbeddingDimensions)]
        public string Embedding => this.Text;
    }
}
