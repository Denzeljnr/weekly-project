using System.Text;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;
using SemanticSearch.Services.Gemini;

namespace SemanticSearch.Services;

public class RerankingService : IRerankingService
{
    private readonly IGeminiTextClient _gemini;

    public RerankingService(IGeminiTextClient gemini) => _gemini = gemini;

    public async Task<List<RankedChunk>> RerankAsync(string question, List<Chunk> candidates)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Question: {question}");
        prompt.AppendLine("\nRate each passage's relevance to the question, 0-10. Respond ONLY with a JSON array of integers, same order as given, no other text.\n");
        for (int i = 0; i < candidates.Count; i++)
            prompt.AppendLine($"[{i}] {candidates[i].Content}");

        var raw = await _gemini.GenerateAsync(prompt.ToString());
        var scores = JsonExtractor.Parse<List<int>>(raw) ?? new List<int>();

        return candidates
            .Select((c, i) => new RankedChunk(c, i < scores.Count ? scores[i] : 0))
            .OrderByDescending(r => r.Relevance)
            .Take(5)
            .ToList();
    }
}