using ChromaDB.Client;
using CommunityToolkit.VectorData.Chroma;
using Testcontainers.Chroma;

namespace ChromaDB.AgentFramework.Sample.Tests;

/// <summary>
/// Starts Chroma in a container for the tests, with a vector store that uses <see cref="WordEmbeddingGenerator"/>.
/// </summary>
public sealed class ChromaFixture : IAsyncLifetime
{
    public const int EmbeddingDimensions = 64;

    private readonly ChromaContainer _container = new ChromaBuilder("chromadb/chroma:1.5.9").Build();
    private readonly HttpClient _httpClient = new();

    public ChromaClient ChromaClient { get; private set; } = null!;

    public ChromaVectorStore VectorStore { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ChromaClient = new ChromaClient(new ChromaConfigurationOptions(_container.GetConnectionString()), _httpClient);
        VectorStore = new ChromaVectorStore(ChromaClient, ownsClient: false, new ChromaVectorStoreOptions { EmbeddingGenerator = new WordEmbeddingGenerator(EmbeddingDimensions) });
    }

    public async ValueTask DisposeAsync()
    {
        VectorStore.Dispose();
        _httpClient.Dispose();
        await _container.DisposeAsync();
    }
}
