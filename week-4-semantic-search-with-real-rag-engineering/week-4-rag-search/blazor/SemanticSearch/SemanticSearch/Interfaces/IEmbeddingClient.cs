using Pgvector;

namespace SemanticSearch.Interfaces;

public interface IEmbeddingClient
{
    Task<Vector> EmbedAsync(string text);
    Task<List<Vector>> EmbedBatchAsync(List<string> texts);
}