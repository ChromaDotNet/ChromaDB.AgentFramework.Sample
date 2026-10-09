using ChromaDB.Client;
using CommunityToolkit.VectorData.Chroma;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Samples;
using Microsoft.Extensions.AI;

namespace ChromaDB.AgentFramework.Sample.Tests;

/// <summary>
/// The scenario of AgentWithRAG_Step07_ChromaBasicTextRAG, with the same TextSearchStore, documents, questions, TextSearchProvider options
/// and chat history filter, against Chroma in a container and without a model.
/// </summary>
public sealed class BasicTextRagTests(ChromaFixture chroma) : IClassFixture<ChromaFixture>
{
    private const string CollectionName = "product-and-policy-info";

    // With the 64 dimensions of the fixture, the hashes of the words of these documents collide,
    // and the return policy ends up nearer to the question about shipping than the shipping guide.
    private const int EmbeddingDimensions = 256;
    private const string ReturnPolicy = "Customers may return any item within 30 days of delivery.";
    private const string ShippingGuide = "Standard shipping is free on orders over $50 and typically arrives in 3-5 business days";
    private const string TentCare = "Clean the tent fabric with lukewarm water and a non-detergent soap.";

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Gives_the_model_the_document_that_answers_each_question()
    {
        var model = new RecordingChatClient();
        var agent = await CreateAgentAsync(model);
        var session = await agent.CreateSessionAsync(_cancellationToken);

        await agent.RunAsync("Hi! I need help understanding the return policy.", session, cancellationToken: _cancellationToken);
        Assert.Contains(ReturnPolicy, model.LastMessagesText);
        Assert.Contains("https://contoso.com/policies/returns", model.LastMessagesText);

        await agent.RunAsync("How long does standard shipping usually take?", session, cancellationToken: _cancellationToken);
        Assert.Contains(ShippingGuide, model.LastMessagesText);

        await agent.RunAsync("What is the best way to maintain the TrailRunner tent fabric?", session, cancellationToken: _cancellationToken);
        Assert.Contains(TentCare, model.LastMessagesText);
    }

    [Fact]
    public async Task Does_not_store_the_search_results_in_the_chat_history()
    {
        var model = new RecordingChatClient();
        var agent = await CreateAgentAsync(model);
        var session = await agent.CreateSessionAsync(_cancellationToken);

        await agent.RunAsync("Hi! I need help understanding the return policy.", session, cancellationToken: _cancellationToken);
        await agent.RunAsync("How long does standard shipping usually take?", session, cancellationToken: _cancellationToken);

        // The second request has the earlier question from the chat history, but not the document found for it.
        Assert.Contains("Hi! I need help understanding the return policy.", model.LastMessagesText);
        Assert.DoesNotContain(ReturnPolicy, model.LastMessagesText);
    }

    [Fact]
    public async Task Stores_the_documents_in_Chroma_with_their_source()
    {
        await CreateAgentAsync(new RecordingChatClient());

        var collection = chroma.ChromaClient.GetCollectionClient(await chroma.ChromaClient.GetCollectionAsync(CollectionName, cancellationToken: _cancellationToken));
        var records = await collection.GetAsync(include: ChromaGetInclude.Metadatas | ChromaGetInclude.Documents, cancellationToken: _cancellationToken);

        Assert.Equal(["return-policy-001", "shipping-guide-001", "tent-care-001"], records.Select(record => (string)record.Metadata!["SourceId"]).Order());
        Assert.Contains(records, record => (string)record.Metadata!["SourceName"] == "Contoso Outdoors Return Policy" && record.Document!.StartsWith(ReturnPolicy));
        Assert.All(records, record => Assert.True(Guid.TryParse(record.Id, out _)));
    }

    // The configuration of the sample: the TextSearchStore of the sample, the single top result before each call,
    // and a chat history that does not store the search results.
    private async Task<AIAgent> CreateAgentAsync(RecordingChatClient model)
    {
        var vectorStore = new ChromaVectorStore(chroma.ChromaClient, ownsClient: false, new ChromaVectorStoreOptions { EmbeddingGenerator = new WordEmbeddingGenerator(EmbeddingDimensions) });
        await vectorStore.EnsureCollectionDeletedAsync(CollectionName, _cancellationToken);
        TextSearchStore textSearchStore = new(vectorStore, CollectionName, EmbeddingDimensions);
        await textSearchStore.UpsertDocumentsAsync(
        [
            new TextSearchDocument { SourceId = "return-policy-001", SourceName = "Contoso Outdoors Return Policy", SourceLink = "https://contoso.com/policies/returns", Text = $"{ReturnPolicy} Items should be unused and include original packaging. Refunds are issued to the original payment method within 5 business days of inspection." },
            new TextSearchDocument { SourceId = "shipping-guide-001", SourceName = "Contoso Outdoors Shipping Guide", SourceLink = "https://contoso.com/help/shipping", Text = $"{ShippingGuide} within the continental United States. Expedited options are available at checkout." },
            new TextSearchDocument { SourceId = "tent-care-001", SourceName = "TrailRunner Tent Care Instructions", SourceLink = "https://contoso.com/manuals/trailrunner-tent", Text = $"{TentCare} Allow it to air dry completely before storage and avoid prolonged UV exposure to extend the lifespan of the waterproof coating." },
        ], cancellationToken: _cancellationToken);

        async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(string text, CancellationToken ct)
        {
            var searchResults = await textSearchStore.SearchAsync(text, 1, ct);
            return searchResults.Select(r => new TextSearchProvider.TextSearchResult { SourceName = r.SourceName, SourceLink = r.SourceLink, Text = r.Text ?? string.Empty, RawRepresentation = r });
        }

        return model.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new() { Instructions = "You are a helpful support specialist for Contoso Outdoors. Answer questions using the provided context and cite the source document when available." },
            AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, new TextSearchProviderOptions { SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke })],
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
            {
                StorageInputRequestMessageFilter = messages => messages.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider && m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory)
            }),
        });
    }
}
