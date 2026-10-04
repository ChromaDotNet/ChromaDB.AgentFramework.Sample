using ChromaDB.Client;
using ChromaDB.VectorData;
using LibraryAssistant;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

var chromaUri = Setting("CHROMA_URI", "http://localhost:8000");
var ollamaUri = Setting("OLLAMA_URI", "http://localhost:11434");
var chatModel = Setting("CHAT_MODEL", "qwen2.5:3b");
var embeddingModel = Setting("EMBEDDING_MODEL", "all-minilm");
var embeddingDimensions = int.Parse(Setting("EMBEDDING_DIMENSIONS", "384"));

using IChatClient chatClient = new OllamaApiClient(ollamaUri, chatModel);
using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaApiClient(ollamaUri, embeddingModel);
using var httpClient = new HttpClient();
using var vectorStore = new ChromaVectorStore(
    new ChromaClient(new ChromaConfigurationOptions(chromaUri), httpClient),
    new ChromaVectorStoreOptions { EmbeddingGenerator = embeddingGenerator });

// Start from an empty memory, so that every run of the sample shows the same conversation.
await vectorStore.EnsureCollectionDeletedAsync(LibraryAgent.MemoryCollectionName);

using var library = await LibraryAgent.CreateAsync(chatClient, vectorStore, embeddingDimensions, readerId: "ada");

Console.WriteLine("First session");
var firstSession = await library.Agent.CreateSessionAsync();
await AskAsync(firstSession, "Hi, I'm Ada. I love science fiction novels.");
await AskAsync(firstSession, "Can I bring my dog to the library?");
await AskAsync(firstSession, "Until what time is the library open on Saturday?");

// A new session has no chat history: what the agent knows about Ada comes from the memory stored in Chroma.
Console.WriteLine();
Console.WriteLine("Second session");
var secondSession = await library.Agent.CreateSessionAsync();
await AskAsync(secondSession, "Is there an event at the library that I might enjoy?");

async Task AskAsync(AgentSession session, string question)
{
    Console.WriteLine($"> {question}");
    var response = await library.Agent.RunAsync(question, session);
    Console.WriteLine(response.Text);
}

static string Setting(string name, string defaultValue)
    => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : defaultValue;
