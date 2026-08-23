using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IRerankingService
{
    Task<List<RankedChunk>> RerankAsync(string question, List<Chunk> candidates);
}