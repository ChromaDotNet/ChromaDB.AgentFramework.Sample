// Copyright (c) Microsoft. All rights reserved.

// This sample shows how to persist chat history in Chroma using the ChatHistoryMemoryProvider,
// with a chat model and an embedding model that run locally in Ollama.
// The agent can then use chat history from prior conversations to inform responses in new conversations.

using ChromaDB.Client;
using ChromaDB.VectorData;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var modelName = Environment.GetEnvironmentVariable("OLLAMA_MODEL_NAME") ?? "qwen2.5:3b";
var embeddingModelName = Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL_NAME") ?? "nomic-embed-text";
var embeddingDimensions = 768;
if (Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_DIMENSIONS") is string embeddingDimensionsValue &&
    (!int.TryParse(embeddingDimensionsValue, out embeddingDimensions) || embeddingDimensions <= 0))
{
    throw new InvalidOperationException("OLLAMA_EMBEDDING_DIMENSIONS must be a positive integer.");
}
var chromaEndpoint = Environment.GetEnvironmentVariable("CHROMA_ENDPOINT") ?? "http://localhost:8000";

using IChatClient chatClient = new OllamaApiClient(new Uri(endpoint), modelName);
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(new Uri(endpoint), embeddingModelName);

using HttpClient httpClient = new();
using ChromaVectorStore vectorStore = new(
    new ChromaClient(new ChromaConfigurationOptions(chromaEndpoint), httpClient),
    ownsClient: true,
    new ChromaVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator,
    });

var userId = $"sample-{Guid.NewGuid():N}";

// Create the agent and add the ChatHistoryMemoryProvider to store chat messages in Chroma.
AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        ChatOptions = new() { Instructions = "You are good at telling jokes." },
        Name = "Joker",
        AIContextProviders = [new ChatHistoryMemoryProvider(
            vectorStore,
            // A collection of its own: the local embedding model has fewer dimensions than the Foundry one.
            collectionName: "chathistory-local",
            vectorDimensions: embeddingDimensions,
            // Callback to configure the initial state of the ChatHistoryMemoryProvider.
            // The ChatHistoryMemoryProvider stores its state in the AgentSession and this callback
            // will be called whenever the ChatHistoryMemoryProvider cannot find existing state in the session,
            // typically the first time it is used with a new session.
            _ => new ChatHistoryMemoryProvider.State(
                // Configure the scope values under which chat messages will be stored.
                // In this case, we are using a per-run user ID and a unique session ID for each new session.
                storageScope: new() { UserId = userId, SessionId = Guid.NewGuid().ToString("N") },
                // Configure the scope which would be used to search for relevant prior messages.
                // In this case, we are searching for any messages for the user across all sessions.
                searchScope: new() { UserId = userId }))]
    });

// Start a new session for the agent conversation.
AgentSession session = await agent.CreateSessionAsync();

// Run the agent with the session that stores conversation history in Chroma.
Console.WriteLine("First session:");
Console.WriteLine(await agent.RunAsync("I like jokes about Pirates. Tell me a joke about a pirate.", session));

// Start a second session. Since we configured the search scope to be across all sessions for the user,
// the agent should remember that the user likes pirate jokes.
AgentSession session2 = await agent.CreateSessionAsync();

// Run the agent with the second session.
Console.WriteLine("Second session (recalling prior chat history from Chroma):");
Console.WriteLine(await agent.RunAsync("Tell me a joke that I might like.", session2));
