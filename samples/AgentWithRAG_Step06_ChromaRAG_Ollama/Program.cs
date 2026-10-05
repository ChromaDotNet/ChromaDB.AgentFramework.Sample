// Copyright (c) Microsoft. All rights reserved.

// This sample shows how to use Chroma with a custom schema to add retrieval augmented generation (RAG) capabilities to an AI agent,
// with a chat model and an embedding model that run locally in Ollama.
// The TextSearchProvider runs a search against the vector store before each model invocation and injects the results into the model context.

using ChromaDB.Client;
using ChromaDB.VectorData;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "qwen2.5:3b";
var embeddingModelName = Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL_NAME") ?? "nomic-embed-text";
var chromaEndpoint = Environment.GetEnvironmentVariable("CHROMA_ENDPOINT") ?? "http://localhost:8000";
var afOverviewUrl = "https://raw.githubusercontent.com/MicrosoftDocs/semantic-kernel-docs/refs/heads/main/agent-framework/overview/index.md";
var afMigrationUrl = "https://raw.githubusercontent.com/MicrosoftDocs/semantic-kernel-docs/refs/heads/main/agent-framework/migration-guide/from-semantic-kernel/index.md";

using IChatClient chatClient = new OllamaApiClient(new Uri(endpoint), modelName);
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri(endpoint), embeddingModelName);

// Create a Chroma vector store that uses the Ollama embedding model to generate embeddings.
using HttpClient httpClient = new();
using ChromaVectorStore vectorStore = new(
    new ChromaClient(new ChromaConfigurationOptions(chromaEndpoint), httpClient),
    new ChromaVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

// Create a collection and upsert some text into it.
// A collection of its own: the local embedding model has fewer dimensions than the Foundry one.
var documentationCollection = vectorStore.GetCollection<Guid, DocumentationChunk>("documentation-local");
await documentationCollection.EnsureCollectionDeletedAsync(); // Clear out any data from previous runs.
await documentationCollection.EnsureCollectionExistsAsync();
await UploadDataFromMarkdown(afOverviewUrl, "Microsoft Agent Framework Overview", documentationCollection, 2000, 200);
await UploadDataFromMarkdown(afMigrationUrl, "Semantic Kernel to Microsoft Agent Framework Migration Guide", documentationCollection, 2000, 200);

// Create an adapter function that the TextSearchProvider can use to run searches against the collection.
async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapterAsync(string text, CancellationToken ct)
{
    List<TextSearchProvider.TextSearchResult> results = [];
    await foreach (var result in documentationCollection.SearchAsync(text, 5, cancellationToken: ct))
    {
        results.Add(new TextSearchProvider.TextSearchResult
        {
            SourceName = result.Record.SourceName,
            SourceLink = result.Record.SourceLink,
            Text = result.Record.Text ?? string.Empty,
            RawRepresentation = result
        });
    }
    return results;
}

// Configure the options for the TextSearchProvider.
TextSearchProviderOptions textSearchOptions = new()
{
    // Run the search prior to every model invocation.
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
    // Use up to 5 recent messages when searching so that searches
    // still produce valuable results even when the user is referring
    // back to previous messages in their request.
    RecentMessageMemoryLimit = 5
};

// Create the AI agent with the TextSearchProvider as the AI context provider.
AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = "You are a helpful support specialist for the Microsoft Agent Framework. Answer questions using the provided context and cite the source document when available. Keep responses brief.",
            // Five passages of documentation and the chat history do not fit in the default context window of Ollama,
            // which would cut the start of the prompt.
            AdditionalProperties = new() { ["num_ctx"] = 16384 }
        },
        AIContextProviders = [new TextSearchProvider(SearchAdapterAsync, textSearchOptions)],
        // Configure a filter on the InMemoryChatHistoryProvider so that we don't persist the messages produced by the TextSearchProvider in chat history.
        // The default is to persist all messages except those that came from chat history in the first place.
        // You may choose to persist the TextSearchProvider messages, if you want the search output to be provided to the model in future interactions as well.
        ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
        {
            StorageInputRequestMessageFilter = msgs => msgs.Where(m => m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.ChatHistory && m.GetAgentRequestMessageSourceType() != AgentRequestMessageSourceType.AIContextProvider)
        })
    });

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(">> Asking about SK sessions\n");
Console.WriteLine(await agent.RunAsync("Hi! How do I create a thread/session in Semantic Kernel?", session));

// Here we are asking a very vague question when taken out of context,
// but since we are including previous messages in our search using RecentMessageMemoryLimit
// the RAG search should still produce useful results.
Console.WriteLine("\n>> Asking about AF sessions\n");
Console.WriteLine(await agent.RunAsync("and in Agent Framework?", session));

Console.WriteLine("\n>> Contrasting Approaches\n");
Console.WriteLine(await agent.RunAsync("Please contrast the two approaches", session));

Console.WriteLine("\n>> Asking about ancestry\n");
Console.WriteLine(await agent.RunAsync("What are the predecessors to the Agent Framework?", session));

static async Task UploadDataFromMarkdown(string markdownUrl, string sourceName, VectorStoreCollection<Guid, DocumentationChunk> vectorStoreCollection, int chunkSize, int overlap)
{
    // Download the markdown from the given url.
    using HttpClient client = new();
    var markdown = await client.GetStringAsync(new Uri(markdownUrl));

    // Chunk it into separate parts with some overlap between chunks
    var chunks = new List<DocumentationChunk>();
    for (int i = 0; i < markdown.Length; i += chunkSize)
    {
        var chunk = new DocumentationChunk
        {
            Key = Guid.NewGuid(),
            SourceLink = markdownUrl,
            SourceName = sourceName,
            Text = markdown.Substring(i, Math.Min(chunkSize + overlap, markdown.Length - i))
        };
        chunks.Add(chunk);
    }

    // Upsert each chunk into the provided vector store.
    await vectorStoreCollection.UpsertAsync(chunks);
}

// Data model that defines the database schema we want to use.
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
    [VectorStoreVector(dimensions: 768)]
    public string Embedding => this.Text;
}
