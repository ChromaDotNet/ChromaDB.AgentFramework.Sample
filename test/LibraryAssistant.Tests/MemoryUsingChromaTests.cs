using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace LibraryAssistant.Tests;

/// <summary>
/// The scenario of AgentWithMemory_Step10_MemoryUsingChroma, with the same ChatHistoryMemoryProvider configuration,
/// against Chroma in a container and without a model.
/// </summary>
public sealed class MemoryUsingChromaTests(ChromaFixture chroma) : IClassFixture<ChromaFixture>
{
    private const string CollectionName = "chathistory";

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Recalls_the_preference_of_the_user_in_a_new_session()
    {
        var model = new RecordingChatClient();
        var (agent, memory) = CreateAgent(model, NewUser());
        using var _ = memory;

        await agent.RunAsync("I like jokes about Pirates. Tell me a joke about a pirate.", await agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);
        await agent.RunAsync("Tell me a joke that I might like.", await agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);

        Assert.Contains("I like jokes about Pirates.", model.LastMessagesText);
    }

    [Fact]
    public async Task Does_not_recall_the_messages_of_another_user()
    {
        var model = new RecordingChatClient();
        var (first, firstMemory) = CreateAgent(model, NewUser());
        var (second, secondMemory) = CreateAgent(model, NewUser());
        using var _ = firstMemory;
        using var __ = secondMemory;

        await first.RunAsync("I like jokes about Pirates. Tell me a joke about a pirate.", await first.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);
        await second.RunAsync("Tell me a joke that I might like.", await second.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);

        Assert.DoesNotContain("Pirates", model.LastMessagesText);
    }

    [Fact]
    public async Task Stores_each_session_under_its_own_session_id_and_the_same_user()
    {
        var userId = NewUser();
        var (agent, memory) = CreateAgent(new RecordingChatClient(), userId);
        using var _ = memory;

        await agent.RunAsync("I like jokes about Pirates.", await agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);
        await agent.RunAsync("Tell me a joke that I might like.", await agent.CreateSessionAsync(_cancellationToken), cancellationToken: _cancellationToken);

        var collection = chroma.ChromaClient.GetCollectionClient(await chroma.ChromaClient.GetCollection(CollectionName, cancellationToken: _cancellationToken));
        var records = await collection.Get(where: ChromaDB.Client.ChromaWhereOperator.Equal("UserId", userId), include: ChromaDB.Client.ChromaGetInclude.Metadatas, cancellationToken: _cancellationToken);

        Assert.Equal(4, records.Count);
        Assert.Equal(2, records.Select(record => (string)record.Metadata!["SessionId"]).Distinct().Count());
    }

    // The ChatHistoryMemoryProvider of the sample: messages stored per user and session, searched per user.
    private (AIAgent Agent, ChatHistoryMemoryProvider Memory) CreateAgent(IChatClient model, string userId)
    {
        var memory = new ChatHistoryMemoryProvider(
            chroma.VectorStore,
            collectionName: CollectionName,
            vectorDimensions: ChromaFixture.EmbeddingDimensions,
            _ => new ChatHistoryMemoryProvider.State(
                storageScope: new() { UserId = userId, SessionId = Guid.NewGuid().ToString("N") },
                searchScope: new() { UserId = userId }));

        var agent = model.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new() { Instructions = "You are good at telling jokes." },
            Name = "Joker",
            AIContextProviders = [memory],
        });

        return (agent, memory);
    }

    private static string NewUser() => $"sample-{Guid.NewGuid():N}";
}
