using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface ISearchClient
{
    Task<ResearchDraft> SearchAsync(string topic, List<CritiqueIssue>? feedbackToAddress = null);
}