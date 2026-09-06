using WriteAngle.Interfaces;
using WriteAngle.Models;
using WriteAngle.Services.Gemini;

namespace WriteAngle.Services.Search;

public class GeminiSearchClient : ISearchClient
{
    private readonly IGeminiTextClient _gemini;

    public GeminiSearchClient(IGeminiTextClient gemini) => _gemini = gemini;

    public async Task<ResearchDraft> SearchAsync(string topic, List<CritiqueIssue>? feedbackToAddress = null)
    {
        var systemPrompt = @"You are a research assistant. Use web search to find real, current information
on the given topic. Respond ONLY in this exact JSON format, no other text:
{ ""claims"": [ { ""claim"": ""string"", ""sourceUrl"": ""string"", ""sourceTitle"": ""string"" } ] }
Every claim must be something you can directly attribute to a real search result. Do not include
claims you cannot tie to a specific source.";

        var userPrompt = feedbackToAddress == null || feedbackToAddress.Count == 0
            ? $"Research this topic: {topic}"
            : $"Research this topic again: {topic}\n\nAddress these specific issues from the previous draft:\n" +
              string.Join("\n", feedbackToAddress.Select(f => $"- {f.Description} (related to: {f.RelatedClaim})"));

        var raw = await _gemini.GenerateAsync(systemPrompt, userPrompt, groundedSearch: true);
        var parsed = JsonExtractor.Parse<Dictionary<string, List<SourcedClaim>>>(raw);
        var claims = parsed != null && parsed.TryGetValue("claims", out var c) ? c : new List<SourcedClaim>();

        return new ResearchDraft(claims);
    }
}