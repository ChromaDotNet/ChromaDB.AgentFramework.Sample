using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace LibraryAssistant.Tests;

/// <summary>
/// Deterministic embeddings without a model: each word adds one to a dimension chosen by its hash,
/// so texts that share words are near each other.
/// </summary>
public sealed class WordEmbeddingGenerator(int dimensions) : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly char[] s_separators = [' ', ',', '.', '?', '!', '\''];

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(values.Select(Embed)));

    private Embedding<float> Embed(string text)
    {
        var vector = new float[dimensions];
        foreach (var word in text.ToLowerInvariant().Split(s_separators).Where(word => word.Length > 2))
        {
            vector[BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(word))) % dimensions] += 1;
        }

        var norm = MathF.Sqrt(vector.Sum(value => value * value));
        if (norm == 0)
        {
            vector[0] = 1;
            norm = 1;
        }

        return new(vector.Select(value => value / norm).ToArray());
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
