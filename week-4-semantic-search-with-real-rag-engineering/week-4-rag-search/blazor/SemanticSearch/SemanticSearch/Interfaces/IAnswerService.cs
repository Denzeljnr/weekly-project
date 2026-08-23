using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IAnswerService
{
    Task<string> AnswerAsync(string question, List<RankedChunk> rankedChunks);
}