using Pgvector;

namespace SemanticSearch.Models;

public class Chunk
{
    public int Id { get; set; }
    public string SourceDocument { get; set; } = "";
    public string Content { get; set; } = "";
    public string ChunkingStrategy { get; set; } = "";
    public Vector Embedding { get; set; } = null!;
}