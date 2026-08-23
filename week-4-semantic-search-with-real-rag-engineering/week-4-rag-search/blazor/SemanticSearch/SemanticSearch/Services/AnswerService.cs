using System.Text;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;

namespace SemanticSearch.Services;

public class AnswerService : IAnswerService
{
    private readonly IGeminiTextClient _gemini;

    public AnswerService(IGeminiTextClient gemini) => _gemini = gemini;

    public async Task<string> AnswerAsync(string question, List<RankedChunk> rankedChunks)
    {
        var context = new StringBuilder();
        foreach (var r in rankedChunks)
            context.AppendLine($"[Source: {r.Chunk.SourceDocument}] {r.Chunk.Content}\n");

        var prompt = $"Answer the question using only the passages below. Cite which source each part of your answer comes from.\n\nQuestion: {question}\n\nPassages:\n{context}";
        return await _gemini.GenerateAsync(prompt);
    }
}