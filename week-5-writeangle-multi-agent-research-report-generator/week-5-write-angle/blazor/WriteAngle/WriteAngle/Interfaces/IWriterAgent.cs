using WriteAngle.Models;

namespace WriteAngle.Interfaces;

public interface IWriterAgent
{
    Task<string> WriteAsync(string topic, ResearchDraft approvedDraft);
}