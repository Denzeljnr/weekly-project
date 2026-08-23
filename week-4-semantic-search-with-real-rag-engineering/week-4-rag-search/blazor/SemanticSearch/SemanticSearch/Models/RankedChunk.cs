namespace SemanticSearch.Models;

// pairs a Chunk with its relevance score from the reranking step
public record RankedChunk(Chunk Chunk, int Relevance);