// Copyright (c) Microsoft. All rights reserved.

// This sample shows how to use TextSearchProvider to add retrieval augmented generation (RAG) capabilities to an AI agent, with Chroma as the vector store,
// and with a chat model and an embedding model that run locally in Ollama.
// The TextSearchProvider runs a search against the vector store via the TextSearchStore before each model invocation and injects the results into the model context.
// The TextSearchStore is a sample store implementation that hardcodes a storage schema and uses the vector store to store and retrieve documents.

using ChromaDB.Client;
using CommunityToolkit.VectorData.Chroma;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Samples;
using Microsoft.Extensions.AI;
using OllamaSharp;

var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "qwen2.5:3b";
var embeddingModelName = Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL_NAME") ?? "nomic-embed-text";
var embeddingDimensions = int.Parse(Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_DIMENSIONS") ?? "768");
var chromaEndpoint = Environment.GetEnvironmentVariable("CHROMA_ENDPOINT") ?? "http://localhost:8000";

using IChatClient chatClient = new OllamaApiClient(new Uri(endpoint), modelName);
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri(endpoint), embeddingModelName);

// Create a Chroma vector store that uses the Ollama embedding model to generate embeddings.
using HttpClient httpClient = new();
using ChromaVectorStore vectorStore = new(
    new ChromaClient(new ChromaConfigurationOptions(chromaEndpoint), httpClient),
    ownsClient: true,
    new ChromaVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

// Create a store that defines a storage schema, and uses the vector store to store and retrieve documents.
// A collection of its own: the local embedding model has fewer dimensions than the Foundry one.
await vectorStore.EnsureCollectionDeletedAsync("product-and-policy-info-local"); // Clear out any data from previous runs.
TextSearchStore textSearchStore = new(vectorStore, "product-and-policy-info-local", embeddingDimensions);

// Upload sample documents into the store.
await textSearchStore.UpsertDocumentsAsync(GetSampleDocuments());

// Create an adapter function that the TextSearchProvider can use to run searches against the TextSearchStore.
async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(string text, CancellationToken ct)
{
    // Here we are limiting the search results to the single top result to demonstrate that we are accurately matching
    // specific search results for each question, but in a real world case, more results should be used.
    var searchResults = await textSearchStore.SearchAsync(text, 1, ct);
    return searchResults.Select(r => new TextSearchProvider.TextSearchResult
    {
        SourceName = r.SourceName,
        SourceLink = r.SourceLink,
        Text = r.Text ?? string.Empty,
        RawRepresentation = r
    });
}

// Configure the options for the TextSearchProvider.
TextSearchProviderOptions textSearchOptions = new()
{
    // Run the search prior to every model invocation.
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
};

// Create the AI agent with the TextSearchProvider as the AI context provider.
AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are a helpful support specialist for Contoso Outdoors. Answer questions using the provided context and cite the source document when available." },
        AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, textSearchOptions)],
        // Since we are using ChatCompletion which stores chat history locally, we can also add a message filter
        // that removes messages produced by the TextSearchProvider before they are added to the chat history, so that
        // we don't bloat chat history with all the search result messages.
        // By default the chat history provider will store all messages, except for those that came from chat history in the first place.
        // We also want to maintain that exclusion here.
        ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
        {
            StorageInputRequestMessageFilter = messages => messages.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider && m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory)
        }),
    });

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(">> Asking about returns\n");
Console.WriteLine(await agent.RunAsync("Hi! I need help understanding the return policy.", session));

Console.WriteLine("\n>> Asking about shipping\n");
Console.WriteLine(await agent.RunAsync("How long does standard shipping usually take?", session));

Console.WriteLine("\n>> Asking about product care\n");
Console.WriteLine(await agent.RunAsync("What is the best way to maintain the TrailRunner tent fabric?", session));

// Produces some sample search documents.
// Each one contains a source name and link, which the agent can use to cite sources in its responses.
static IEnumerable<TextSearchDocument> GetSampleDocuments()
{
    yield return new TextSearchDocument
    {
        SourceId = "return-policy-001",
        SourceName = "Contoso Outdoors Return Policy",
        SourceLink = "https://contoso.com/policies/returns",
        Text = "Customers may return any item within 30 days of delivery. Items should be unused and include original packaging. Refunds are issued to the original payment method within 5 business days of inspection."
    };
    yield return new TextSearchDocument
    {
        SourceId = "shipping-guide-001",
        SourceName = "Contoso Outdoors Shipping Guide",
        SourceLink = "https://contoso.com/help/shipping",
        Text = "Standard shipping is free on orders over $50 and typically arrives in 3-5 business days within the continental United States. Expedited options are available at checkout."
    };
    yield return new TextSearchDocument
    {
        SourceId = "tent-care-001",
        SourceName = "TrailRunner Tent Care Instructions",
        SourceLink = "https://contoso.com/manuals/trailrunner-tent",
        Text = "Clean the tent fabric with lukewarm water and a non-detergent soap. Allow it to air dry completely before storage and avoid prolonged UV exposure to extend the lifespan of the waterproof coating."
    };
}
