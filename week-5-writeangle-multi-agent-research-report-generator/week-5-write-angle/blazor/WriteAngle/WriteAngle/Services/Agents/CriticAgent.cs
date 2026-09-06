using WriteAngle.Interfaces;
using WriteAngle.Models;
using WriteAngle.Services.Gemini;

namespace WriteAngle.Services.Agents;

public class CriticAgent : ICriticAgent
{
    private readonly IGeminiTextClient _gemini;

    public CriticAgent(IGeminiTextClient gemini) => _gemini = gemini;

    public async Task<CritiqueResult> ReviewAsync(ResearchDraft draft)
    {
        var systemPrompt = @"You are a skeptical fact-checking editor. For each claim below, judge whether
it is genuinely well-supported by its cited source, based on the source's title and URL alone (you do not
have the full source text, so judge plausibility of the claim-to-source fit, not just whether the claim
sounds true). Flag anything vague, unsupported, or where the source doesn't obviously match the claim.

Respond ONLY in this exact JSON format, no other text:
{ ""approved"": true or false, ""issues"": [ { ""description"": ""string"", ""relatedClaim"": ""string"" } ] }
Set approved to true only if there are no issues.";

        var userPrompt = string.Join("\n", draft.Claims.Select(c => $"Claim: {c.Claim}\nSource: {c.SourceTitle} ({c.SourceUrl})\n"));

        var raw = await _gemini.GenerateAsync(systemPrompt, userPrompt);
        return JsonExtractor.Parse<CritiqueResult>(raw) ?? new CritiqueResult(true, new List<CritiqueIssue>());
    }
}