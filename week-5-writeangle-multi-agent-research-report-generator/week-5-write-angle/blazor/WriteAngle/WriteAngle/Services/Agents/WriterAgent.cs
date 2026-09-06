using WriteAngle.Interfaces;
using WriteAngle.Models;

namespace WriteAngle.Services.Agents;

public class WriterAgent : IWriterAgent
{
    private readonly IGeminiTextClient _gemini;

    public WriterAgent(IGeminiTextClient gemini) => _gemini = gemini;

    public async Task<string> WriteAsync(string topic, ResearchDraft approvedDraft)
    {
        var systemPrompt = "You are a professional research writer. Produce a clean, well-organized report " +
            "using only the claims and sources provided. Cite the source title after each claim you use.";

        var userPrompt = $"Topic: {topic}\n\nApproved claims:\n" +
            string.Join("\n", approvedDraft.Claims.Select(c => $"- {c.Claim} [Source: {c.SourceTitle}]"));

        return await _gemini.GenerateAsync(systemPrompt, userPrompt);
    }
}