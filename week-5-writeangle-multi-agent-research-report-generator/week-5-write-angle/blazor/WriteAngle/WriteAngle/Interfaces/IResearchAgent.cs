using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface IResearchAgent
{
    Task<ResearchDraft> ResearchAsync(string topic, List<CritiqueIssue>? feedbackToAddress = null);
}