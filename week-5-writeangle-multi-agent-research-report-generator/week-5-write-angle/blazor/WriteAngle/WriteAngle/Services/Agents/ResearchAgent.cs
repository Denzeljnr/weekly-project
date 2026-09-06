using WriteAngle.Interfaces;
using WriteAngle.Models;

namespace WriteAngle.Services.Agents;

public class ResearchAgent : IResearchAgent
{
    private readonly ISearchClient _searchClient;

    public ResearchAgent(ISearchClient searchClient) => _searchClient = searchClient;

    public Task<ResearchDraft> ResearchAsync(string topic, List<CritiqueIssue>? feedbackToAddress = null)
        => _searchClient.SearchAsync(topic, feedbackToAddress);
}