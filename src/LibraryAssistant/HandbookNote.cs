using Microsoft.Extensions.VectorData;

namespace LibraryAssistant;

/// <summary>
/// A passage of the library handbook, stored in Chroma with the embedding of its text.
/// </summary>
public sealed class HandbookNote
{
    public string Id { get; set; } = "";

    public string Topic { get; set; } = "";

    public string Text { get; set; } = "";

    /// <summary>
    /// The text to embed: the embedding generator of the vector store turns it into a vector when the note is stored.
    /// </summary>
    public string Embedding => Text;

    /// <summary>
    /// The schema of the collection, with the number of dimensions of the embedding model.
    /// </summary>
    public static VectorStoreCollectionDefinition Definition(int embeddingDimensions) => new()
    {
        Properties =
        [
            new VectorStoreKeyProperty(nameof(Id), typeof(string)),
            new VectorStoreDataProperty(nameof(Topic), typeof(string)) { IsIndexed = true },
            new VectorStoreDataProperty(nameof(Text), typeof(string)),
            new VectorStoreVectorProperty(nameof(Embedding), typeof(string), embeddingDimensions),
        ],
    };
}
